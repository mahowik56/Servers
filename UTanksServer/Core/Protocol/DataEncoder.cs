using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;

using UTanksServer.Services.Servers.Game;
using UTanksServer.Services.Servers.Game.Connection;

namespace UTanksServer.Core.Protocol {
  public sealed class DataEncoder {
    private readonly ArrayPool<byte> _pool;

    public DataEncoder(ArrayPool<byte>? pool = null) {
      _pool = pool ?? ArrayPool<byte>.Shared;
    }

    public PooledPacket Encode(IReadOnlyList<QueuedCommand> commands, uint sequence, uint ack, uint ackMask) {
      BufferWriter writer = new BufferWriter();
      writer.WriteInt32(0); // Placeholder for total length
      writer.WriteUInt32(sequence);
      writer.WriteUInt32(ack);
      writer.WriteUInt32(ackMask);
      writer.WriteUInt16((ushort) commands.Count);

      for(int i = 0; i < commands.Count; i++) {
        QueuedCommand queued = commands[i];
        ICommand command = queued.Command;
        ICommandCodec codec = CommandCodecRegistry.GetByType(command.GetType());

        BufferWriter payloadWriter = new BufferWriter();
        codec.Encode(ref payloadWriter, command);
        int payloadLength = payloadWriter.WrittenCount;

        byte flags = EncodeFlags(queued.Options);
        writer.WriteUInt16(codec.TypeId);
        writer.WriteByte(flags);
        writer.WriteUInt16((ushort) payloadLength);
        writer.Write(payloadWriter.WrittenMemory.Span);
      }

      int packetLength = writer.WrittenCount - sizeof(int);
      Span<byte> lengthSpan = writer.WrittenMemory.Span.Slice(0, sizeof(int));
      BinaryPrimitives.WriteInt32BigEndian(lengthSpan, packetLength);

      byte[] rented = _pool.Rent(writer.WrittenCount);
      writer.WrittenMemory.Span.CopyTo(rented);
      return new PooledPacket(_pool, rented, writer.WrittenCount);
    }

    private static byte EncodeFlags(CommandSendOptions options) {
      int priority = (int) options.Priority;
      byte flags = (byte) (priority & 0b111);
      if(options.Reliable) {
        flags |= 0b1000;
      }
      return flags;
    }
  }

  public readonly struct PooledPacket : IDisposable {
    private readonly ArrayPool<byte> _pool;
    public byte[] Buffer { get; }
    public int Length { get; }

    public PooledPacket(ArrayPool<byte> pool, byte[] buffer, int length) {
      _pool = pool;
      Buffer = buffer;
      Length = length;
    }

    public void Dispose() {
      _pool.Return(Buffer, clearArray: false);
    }
  }
}
