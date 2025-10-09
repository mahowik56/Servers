using System.Threading.Tasks;

using UTanksServer.Services.Servers.Game;
using UTanksServer.Services.Servers.Game.Connection;

namespace UTanksServer.Core.Protocol.Commands {
  public sealed class HeartbeatCommand : ICommand, ICommandMetadataProvider {
    public long ClientTime { get; init; }

    public Task OnReceive(Player player) {
      player.Connection?.QueueCommands(new HeartbeatCommand { ClientTime = ClientTime });
      return Task.CompletedTask;
    }

    public CommandSendOptions GetSendOptions() => new CommandSendOptions(CommandPriority.Low, reliable: false, estimatedSize: 8);
  }

  public sealed class HeartbeatCodec : ICommandCodec {
    public ushort TypeId => 1;
    public System.Type CommandType => typeof(HeartbeatCommand);

    public void Encode(ref BufferWriter writer, ICommand command) {
      HeartbeatCommand heartbeat = (HeartbeatCommand) command;
      writer.WriteUInt32((uint) heartbeat.ClientTime);
    }

    public ICommand Decode(ref BufferReader reader, Player player) {
      reader.TryReadUInt32(out uint clientTime);
      return new HeartbeatCommand { ClientTime = clientTime };
    }

    public int EstimateSize(ICommand command) => sizeof(uint);
  }
}
