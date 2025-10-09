using System;
using System.Threading;
using System.Threading.Tasks;

namespace UTanksServer.Services.Servers.Game.Connection {
  public interface IPlayerConnection : IAsyncDisposable {
    Player Player { get; }
    bool IsConnected { get; }

    ValueTask InitializeAsync(Player player, GameServer server, CancellationToken token);
    Task RunAsync(CancellationToken token);

    bool QueueCommands(params ICommand[] commands);
    void Disconnect();
  }
}
