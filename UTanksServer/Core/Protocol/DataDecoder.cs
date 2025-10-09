using System;
using System.Collections.Generic;

using UTanksServer.Services.Servers.Game;
using UTanksServer.Services.Servers.Game.Connection;

namespace UTanksServer.Core.Protocol {
  public sealed class DataDecoder {
    public DecodedPacket Decode(ReadOnlyMemory<byte> packet, Player player) {
      BufferReader reader = new BufferReader(packet);
      if(!reader.TryReadInt32(out int length)) {
        throw new InvalidOperationException("Packet truncated");
      }

      if(length != reader.Remaining) {
        throw new InvalidOperationException("Invalid packet length");
      }

      if(!reader.TryReadUInt32(out uint sequence)) {
        throw new InvalidOperationException("Missing sequence");
      }

      if(!reader.TryReadUInt32(out uint ack)) {
        throw new InvalidOperationException("Missing ack");
      }

      if(!reader.TryReadUInt32(out uint ackMask)) {
        throw new InvalidOperationException("Missing ack mask");
      }

      if(!reader.TryReadUInt16(out ushort commandCount)) {
        throw new InvalidOperationException("Missing command count");
      }

      List<ReceivedCommand> commands = new List<ReceivedCommand>(commandCount);
      for(int i = 0; i < commandCount; i++) {
        if(!reader.TryReadUInt16(out ushort typeId)) {
          throw new InvalidOperationException("Missing command type id");
        }

        if(!reader.TryReadByte(out byte flags)) {
          throw new InvalidOperationException("Missing flags");
        }

        if(!reader.TryReadUInt16(out ushort payloadLength)) {
          throw new InvalidOperationException("Missing payload length");
        }

        if(!reader.TryReadBytes(payloadLength, out ReadOnlyMemory<byte> payload)) {
          throw new InvalidOperationException("Truncated payload");
        }

        BufferReader payloadReader = new BufferReader(payload);
        ICommandCodec codec = CommandCodecRegistry.GetById(typeId);
        ICommand command = codec.Decode(ref payloadReader, player);
        CommandPriority priority = (CommandPriority) (flags & 0b111);
        bool reliable = (flags & 0b1000) != 0;
        commands.Add(new ReceivedCommand(command, priority, reliable));
      }

      return new DecodedPacket(sequence, ack, ackMask, commands);
    }
  }

  public readonly record struct ReceivedCommand(ICommand Command, CommandPriority Priority, bool Reliable);
  public readonly record struct DecodedPacket(uint Sequence, uint Ack, uint AckMask, IReadOnlyList<ReceivedCommand> Commands);
}
