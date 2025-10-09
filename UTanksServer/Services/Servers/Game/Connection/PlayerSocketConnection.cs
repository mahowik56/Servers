using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;

using Serilog;

using UTanksServer.Core.Logging;
using UTanksServer.Core.Protocol;
using UTanksServer.Diagnostics;
using UTanksServer.Services.Servers.Game.Security;

namespace UTanksServer.Services.Servers.Game.Connection {
  public sealed class PlayerSocketConnection : IPlayerConnection {
    private static readonly ILogger Logger = Log.Logger.ForContext<PlayerSocketConnection>();

    private readonly Socket _socket;
    private readonly GameServerConfig _config;
    private readonly SendQuotaManager _quotaManager;
    private readonly AntiFloodController _antiFlood;
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    private readonly Queue<QueuedCommand>[] _queues;
    private readonly SemaphoreSlim _sendSignal = new SemaphoreSlim(0);
    private readonly object _queueLock = new object();
    private int _queuedBytes;

    private readonly Pipe _pipe = new Pipe();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    private readonly Dictionary<uint, PendingReliable> _pendingReliables = new Dictionary<uint, PendingReliable>();

    private uint _nextOutboundSequence = 1;
    private uint _lastReceivedSequence;
    private uint _receivedMask;
    private readonly object _ackLock = new object();

    private Task? _sendTask;
    private Task? _receiveTask;

    public Player Player { get; private set; } = null!;
    public bool IsConnected => !_cts.IsCancellationRequested && _socket.Connected;

    public PlayerSocketConnection(Socket socket, GameServerConfig config, SendQuotaManager quotaManager, AntiFloodController antiFlood) {
      _socket = socket;
      _config = config;
      _quotaManager = quotaManager;
      _antiFlood = antiFlood;

      _queues = Enumerable.Range(0, Enum.GetValues(typeof(CommandPriority)).Length)
        .Select(_ => new Queue<QueuedCommand>())
        .ToArray();
    }

    public ValueTask InitializeAsync(Player player, GameServer server, CancellationToken token) {
      Player = player;
      CommandCodecBootstrapper.EnsureInitialized();
      return ValueTask.CompletedTask;
    }

    public async Task RunAsync(CancellationToken token) {
      using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);
      CancellationToken linkedToken = linkedCts.Token;

      _sendTask = Task.Run(() => SendLoopAsync(linkedToken), linkedToken);
      _receiveTask = Task.Run(() => ReceiveLoopAsync(linkedToken), linkedToken);

      await Task.WhenAll(_sendTask, _receiveTask);
    }

    public bool QueueCommands(params ICommand[] commands) {
      bool result = false;
      foreach(ICommand command in commands) {
        CommandSendOptions options = command.GetSendOptions();
        options = options.WithEstimatedSize(Math.Max(options.EstimatedSize, 32));
        try {
          ICommandCodec codec = CommandCodecRegistry.GetByType(command.GetType());
          int estimated = codec.EstimateSize(command);
          if(estimated > 0) {
            options = options.WithEstimatedSize(Math.Max(options.EstimatedSize, estimated));
          }
        } catch(KeyNotFoundException) {
        }
        if(TryEnqueue(command, options)) {
          result = true;
        }
      }

      if(result) {
        _sendSignal.Release();
      }

      return result;
    }

    public void Disconnect() {
      if(_cts.IsCancellationRequested) {
        return;
      }

      _cts.Cancel();
      try {
        _socket.Shutdown(SocketShutdown.Both);
      } catch(SocketException) {
      }
      _socket.Close();
    }

    public async ValueTask DisposeAsync() {
      Disconnect();
      if(_sendTask != null) await _sendTask.ConfigureAwait(false);
      if(_receiveTask != null) await _receiveTask.ConfigureAwait(false);
    }

    private bool TryEnqueue(ICommand command, CommandSendOptions options) {
      if(!_quotaManager.TryReserve(options.Priority, options.EstimatedSize, out int ticket)) {
        if(!EvictLowerPriority(options.EstimatedSize, options.Priority) ||
           !_quotaManager.TryReserve(options.Priority, options.EstimatedSize, out ticket)) {
          return false;
        }
      }

      lock(_queueLock) {
        if(!EnsureClientRoomLocked(options.EstimatedSize, options.Priority)) {
          _quotaManager.Release(ticket);
          return false;
        }

        _queues[(int) options.Priority].Enqueue(new QueuedCommand(command, options, ticket));
        _queuedBytes += options.EstimatedSize;
        return true;
      }
    }

    private bool EnsureClientRoom(int bytes, CommandPriority priority) {
      lock(_queueLock) {
        EvictLowerPriorityLocked(bytes, priority);
        return _queuedBytes + bytes <= _config.PerClientSendQueueBytes;
      }
    }

    private bool EvictLowerPriority(int bytesNeeded, CommandPriority priority) {
      lock(_queueLock) {
        EvictLowerPriorityLocked(bytesNeeded, priority);
        return _queuedBytes + bytesNeeded <= _config.PerClientSendQueueBytes;
      }
    }

    private bool EnsureClientRoomLocked(int bytes, CommandPriority priority) {
      if(_config.PerClientSendQueueBytes <= 0) {
        return true;
      }

      if(_queuedBytes + bytes <= _config.PerClientSendQueueBytes) {
        return true;
      }

      EvictLowerPriorityLocked(bytes, priority);
      return _queuedBytes + bytes <= _config.PerClientSendQueueBytes;
    }

    private void EvictLowerPriorityLocked(int bytesNeeded, CommandPriority priority) {
      if(_queuedBytes + bytesNeeded <= _config.PerClientSendQueueBytes) {
        return;
      }

      for(int p = _queues.Length - 1; p >= (int) priority; p--) {
        Queue<QueuedCommand> queue = _queues[p];
        while(queue.Count > 0 && _queuedBytes + bytesNeeded > _config.PerClientSendQueueBytes) {
          QueuedCommand dropped = queue.Dequeue();
          _quotaManager.Release(dropped.Ticket);
          _queuedBytes -= dropped.Options.EstimatedSize;
          PerfCounters.Increment("network.send.dropped", 1);
        }

        if(_queuedBytes + bytesNeeded <= _config.PerClientSendQueueBytes) {
          return;
        }
      }
    }

    private async Task ReceiveLoopAsync(CancellationToken token) {
      Task writingTask = Task.Run(() => FillPipeAsync(token), token);
      Task readingTask = Task.Run(() => ReadPipeAsync(token), token);
      await Task.WhenAll(writingTask, readingTask);
    }

    private async Task FillPipeAsync(CancellationToken token) {
      try {
        while(!token.IsCancellationRequested) {
          Memory<byte> memory = _pipe.Writer.GetMemory(4096);
          int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, token);
          if(bytesRead == 0) {
            Disconnect();
            break;
          }

          _pipe.Writer.Advance(bytesRead);
          FlushResult result = await _pipe.Writer.FlushAsync(token);
          if(result.IsCompleted) {
            break;
          }
        }
      } catch(OperationCanceledException) {
      } catch(Exception exception) {
        Logger.Error(exception, "Receive loop failed");
        Disconnect();
      } finally {
        await _pipe.Writer.CompleteAsync();
      }
    }

    private async Task ReadPipeAsync(CancellationToken token) {
      try {
        while(true) {
          ReadResult result = await _pipe.Reader.ReadAsync(token);
          ReadOnlySequence<byte> buffer = result.Buffer;
          SequencePosition consumed = buffer.Start;
          SequencePosition examined = buffer.End;

          while(TryReadFrame(ref buffer, out ReadOnlyMemory<byte> packet, out byte[]? rented)) {
            consumed = buffer.Start;
            examined = consumed;
            try {
              await HandlePacketAsync(packet, token);
            } finally {
              if(rented != null) {
                _pool.Return(rented);
              }
            }
          }

          _pipe.Reader.AdvanceTo(consumed, examined);
          if(result.IsCompleted) {
            break;
          }
        }
      } catch(OperationCanceledException) {
      } catch(Exception exception) {
        Logger.Error(exception, "Read loop failed");
        Disconnect();
      } finally {
        await _pipe.Reader.CompleteAsync();
      }
    }

    private bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlyMemory<byte> packet, out byte[]? rented) {
      packet = ReadOnlyMemory<byte>.Empty;
      rented = null;
      if(buffer.Length < sizeof(int)) {
        return false;
      }

      SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
      if(!reader.TryReadBigEndian(out int length)) {
        return false;
      }

      if(reader.Remaining < length) {
        return false;
      }

      ReadOnlySequence<byte> payload = buffer.Slice(reader.Position, length);
      if(payload.IsSingleSegment) {
        packet = payload.First;
      } else {
        byte[] bufferArray = _pool.Rent(length);
        payload.CopyTo(bufferArray);
        packet = new ReadOnlyMemory<byte>(bufferArray, 0, length);
        rented = bufferArray;
      }

      buffer = buffer.Slice(payload.End);
      return true;
    }

    private async Task HandlePacketAsync(ReadOnlyMemory<byte> packet, CancellationToken token) {
      DataDecoder decoder = new DataDecoder();
      DecodedPacket decoded = decoder.Decode(packet, Player);
      UpdateReceiveWindow(decoded.Sequence);
      ProcessAck(decoded.Ack, decoded.AckMask);

      foreach(ReceivedCommand received in decoded.Commands) {
        if(!_antiFlood.TryConsume(Player, received.Command.GetType().Name, 1, DateTime.UtcNow)) {
          Logger.Warning("Flood detected for player {Player}", Player.LogDisplay);
          continue;
        }

        try {
          await received.Command.OnReceive(Player);
        } catch(Exception exception) {
          Logger.Error(exception, "Error executing command");
        }
      }
    }

    private async Task SendLoopAsync(CancellationToken token) {
      List<QueuedCommand> batch = new List<QueuedCommand>(_config.MaxCommandsPerBatch);
      DataEncoder encoder = new DataEncoder(_pool);

      try {
        while(!token.IsCancellationRequested) {
          await _sendSignal.WaitAsync(token);

          if(!TryDequeueBatch(batch, out int _)) {
            continue;
          }

          uint sequence = _nextOutboundSequence++;
          uint ackSequence;
          uint ackMask;
          lock(_ackLock) {
            ackSequence = _lastReceivedSequence;
            ackMask = _receivedMask;
          }
          using IDisposable _ = PerfCounters.TrackDuration("network.send.encode");
          using PooledPacket packet = encoder.Encode(batch, sequence, ackSequence, ackMask);
          await _socket.SendAsync(new ReadOnlyMemory<byte>(packet.Buffer, 0, packet.Length), SocketFlags.None, token);

          RegisterReliables(sequence, batch);
          foreach(QueuedCommand command in batch) {
            _quotaManager.Release(command.Ticket);
          }

          batch.Clear();
          if(HasPendingCommands()) {
            _sendSignal.Release();
          }
          CheckRetransmissions();
        }
      } catch(OperationCanceledException) {
      } catch(Exception exception) {
        Logger.Error(exception, "Send loop failed");
        Disconnect();
      }
    }

    private bool TryDequeueBatch(List<QueuedCommand> batch, out int bytes) {
      batch.Clear();
      bytes = 0;

      lock(_queueLock) {
        int maxBytes = _config.MaxBatchBytes;
        int maxCommands = _config.MaxCommandsPerBatch;

        for(int priority = 0; priority < _queues.Length; priority++) {
          Queue<QueuedCommand> queue = _queues[priority];
          while(queue.Count > 0) {
            QueuedCommand command = queue.Peek();
            if(batch.Count >= maxCommands) {
              return batch.Count > 0;
            }

            if(bytes + command.Options.EstimatedSize > maxBytes && batch.Count > 0) {
              break;
            }

            queue.Dequeue();
            _queuedBytes -= command.Options.EstimatedSize;
            batch.Add(command);
            bytes += command.Options.EstimatedSize;
          }
        }
      }

      return batch.Count > 0;
    }

    private void RegisterReliables(uint sequence, List<QueuedCommand> batch) {
      List<QueuedCommand> reliable = batch.Where(c => c.Options.Reliable).ToList();
      if(reliable.Count == 0) {
        return;
      }

      _pendingReliables[sequence] = new PendingReliable(sequence, reliable, DateTime.UtcNow, attempts: 1);
    }

    private void UpdateReceiveWindow(uint sequence) {
      if(sequence == 0) {
        return;
      }

      lock(_ackLock) {
        if(sequence > _lastReceivedSequence) {
          uint diff = sequence - _lastReceivedSequence;
          if(diff >= 32) {
            _receivedMask = 1;
          } else {
            _receivedMask = (_receivedMask << (int) diff) | 1;
          }

          _lastReceivedSequence = sequence;
        } else {
          uint offset = _lastReceivedSequence - sequence;
          if(offset > 0 && offset <= 32) {
            _receivedMask |= (uint) (1 << (int) (offset - 1));
          }
        }
      }
    }

    private void ProcessAck(uint ack, uint ackMask) {
      lock(_ackLock) {
        _pendingReliables.Remove(ack);

        for(int bit = 0; bit < 32; bit++) {
          if(((ackMask >> bit) & 1) == 1) {
            uint sequence = ack - (uint) (bit + 1);
            _pendingReliables.Remove(sequence);
          }
        }
      }
    }

    private void CheckRetransmissions() {
      DateTime now = DateTime.UtcNow;
      List<(uint Sequence, PendingReliable Pending)> toRetransmit = new List<(uint, PendingReliable)>();
      lock(_ackLock) {
        foreach(KeyValuePair<uint, PendingReliable> pair in _pendingReliables) {
          if(now - pair.Value.LastSent >= TimeSpan.FromMilliseconds(200)) {
            toRetransmit.Add((pair.Key, pair.Value));
          }
        }

        foreach((uint Sequence, PendingReliable Pending) item in toRetransmit.ToArray()) {
          if(item.Pending.Attempts >= 5) {
            Logger.Warning("Dropping reliable packet {Sequence} for player {Player}", item.Sequence, Player.LogDisplay);
            _pendingReliables.Remove(item.Sequence);
            toRetransmit.Remove(item);
          }
        }

        foreach((uint Sequence, PendingReliable Pending) item in toRetransmit) {
          _pendingReliables[item.Sequence] = item.Pending with { LastSent = now, Attempts = item.Pending.Attempts + 1 };
        }
      }

      foreach((uint Sequence, PendingReliable Pending) item in toRetransmit) {
        foreach(QueuedCommand command in item.Pending.Commands) {
          QueueCommands(command.Command);
        }
      }
    }

    private bool HasPendingCommands() {
      lock(_queueLock) {
        for(int i = 0; i < _queues.Length; i++) {
          if(_queues[i].Count > 0) {
            return true;
          }
        }
      }

      return false;
    }

    private readonly record struct PendingReliable(uint Sequence, IReadOnlyList<QueuedCommand> Commands, DateTime LastSent, int Attempts);
  }
}
