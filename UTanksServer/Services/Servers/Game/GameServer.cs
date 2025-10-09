using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

using UTanksServer.Core.Logging;
using UTanksServer.Core.Protocol;
using UTanksServer.Diagnostics;
using UTanksServer.Services.Servers.Game.Connection;
using UTanksServer.Services.Servers.Game.Security;
using UTanksServer.Services.Servers.Game.World;

namespace UTanksServer.Services.Servers.Game {
  public interface IGameServer {
    Task StartAsync(CancellationToken cancellationToken);
  }

  public interface ICommand {
    Task OnReceive(Player player);
  }

  [UTanksServer.ECS.ECSCore.Service]
  public sealed class GameServer : IGameServer, IAsyncDisposable {
    private static readonly ILogger Logger = Log.Logger.ForContext<GameServer>();

    public IPEndPoint Endpoint { get; }

    private readonly Socket _listener;
    private readonly ConcurrentDictionary<Guid, PlayerSocketConnection> _connections = new ConcurrentDictionary<Guid, PlayerSocketConnection>();
    private readonly GameServerConfig _config;
    private readonly SendQuotaManager _quotaManager;
    private readonly AntiFloodController _antiFlood = new AntiFloodController();
    private readonly InterestManager _interestManager;
    private readonly SnapshotCache _snapshotCache = new SnapshotCache();

    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    private Task? _acceptTask;
    private Task? _tickTask;

    public GameServer(IConfigService configService) {
      _config = configService.GameServerConfig;
      Endpoint = new IPEndPoint(_config.Address, _config.Port);
      _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      _quotaManager = new SendQuotaManager(_config.GlobalSendQueueBytes);
      _interestManager = new InterestManager(_config.InterestCellSize, _config.InterestRadius);
      CommandCodecBootstrapper.EnsureInitialized();
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
      _listener.Bind(Endpoint);
      _listener.Listen(_config.Backlog);

      ThreadPool.SetMinThreads(50, 50);

      using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
      CancellationToken token = linkedCts.Token;

      _acceptTask = Task.Run(() => AcceptLoopAsync(token), token);
      _tickTask = Task.Run(() => TickLoopAsync(token), token);

      Logger.Information("Started game server: {Address}:{Port}", Endpoint.Address, Endpoint.Port);
      await Task.WhenAll(_acceptTask, _tickTask);
    }

    public async ValueTask DisposeAsync() {
      _cts.Cancel();
      try {
        _listener.Close();
      } catch(SocketException) {
      }

      if(_acceptTask != null) await _acceptTask.ConfigureAwait(false);
      if(_tickTask != null) await _tickTask.ConfigureAwait(false);

      foreach(PlayerSocketConnection connection in _connections.Values) {
        await connection.DisposeAsync();
      }
    }

    private async Task AcceptLoopAsync(CancellationToken token) {
      Logger.Information("Accept loop started");
      try {
        while(!token.IsCancellationRequested) {
          Socket clientSocket = await _listener.AcceptAsync();
          token.ThrowIfCancellationRequested();
          _ = Task.Run(() => HandleClientAsync(clientSocket, token), token);
        }
      } catch(OperationCanceledException) {
      } catch(Exception exception) {
        Logger.Error(exception, "Accept loop failed");
      }
    }

    private async Task HandleClientAsync(Socket socket, CancellationToken token) {
      Player player = new Player();
      PlayerSocketConnection connection = new PlayerSocketConnection(socket, _config, _quotaManager, _antiFlood);
      await connection.InitializeAsync(player, this, token);
      player.Connection = connection;

      Guid id = Guid.NewGuid();
      if(!_connections.TryAdd(id, connection)) {
        Logger.Warning("Failed to track connection for player {Player}", player.LogDisplay);
      }

      Logger.Information("Accepted connection from {Endpoint}", socket.RemoteEndPoint);

      try {
        await connection.RunAsync(token);
      } catch(Exception exception) {
        Logger.Error(exception, "Connection loop failed");
      } finally {
        _connections.TryRemove(id, out _);
        await connection.DisposeAsync();
        Logger.Information("Connection closed: {Endpoint}", socket.RemoteEndPoint);
      }
    }

    private async Task TickLoopAsync(CancellationToken token) {
      TimeSpan tickInterval = TimeSpan.FromSeconds(1.0 / Math.Max(1, _config.TickRate));
      Stopwatch stopwatch = Stopwatch.StartNew();

      try {
        while(!token.IsCancellationRequested) {
          long startTicks = stopwatch.ElapsedTicks;

          using IDisposable _ = PerfCounters.TrackDuration("tick.total");
          RunTick();

          long elapsedTicks = stopwatch.ElapsedTicks - startTicks;
          double elapsedMilliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency;
          PerfCounters.Increment("tick.duration", elapsedMilliseconds);

          TimeSpan sleep = tickInterval - TimeSpan.FromMilliseconds(elapsedMilliseconds);
          if(sleep > TimeSpan.Zero) {
            try {
              await Task.Delay(sleep, token);
            } catch(TaskCanceledException) {
              break;
            }
          }
        }
      } catch(OperationCanceledException) {
      } catch(Exception exception) {
        Logger.Error(exception, "Tick loop failed");
      }
    }

    private void RunTick() {
      ExecutePhase("tick.input", ProcessInputs);
      ExecutePhase("tick.simulation", SimulateWorld);
      ExecutePhase("tick.apply", ApplyResults);
      ExecutePhase("tick.broadcast", BroadcastUpdates);
    }

    private void ExecutePhase(string name, Action action) {
      using IDisposable _ = PerfCounters.TrackDuration(name);
      action();
    }

    private void ProcessInputs() {
      // Inputs are already processed in connection threads; gather metrics here if necessary.
      PerfCounters.Increment("tick.inputs", _connections.Count);
    }

    private void SimulateWorld() {
      // Placeholder for simulation logic. Parallelize heavy systems in the future.
    }

    private void ApplyResults() {
      // Placeholder for applying simulation results to world state.
    }

    private void BroadcastUpdates() {
      foreach(PlayerSocketConnection connection in _connections.Values) {
        if(!connection.IsConnected) {
          continue;
        }

        // For now we send heartbeat to keep the link alive and validate delta path.
        connection.QueueCommands(new Core.Protocol.Commands.HeartbeatCommand { ClientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
      }
    }
  }
}
