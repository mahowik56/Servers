using System;
using System.Buffers;
using System.Buffers.Binary;

namespace UTanksServer.Core.Protocol {
  public sealed class BufferWriter : IBufferWriter<byte>, IBufferWriter {
    private readonly ArrayBufferWriter<byte> _writer = new ArrayBufferWriter<byte>();

    public void Write(ReadOnlySpan<byte> source) {
      Span<byte> destination = GetSpan(source.Length);
      source.CopyTo(destination);
      Advance(source.Length);
    }

    public void WriteByte(byte value) {
      Span<byte> destination = GetSpan(1);
      destination[0] = value;
      Advance(1);
    }

    public void WriteUInt16(ushort value) {
      Span<byte> destination = GetSpan(sizeof(ushort));
      BinaryPrimitives.WriteUInt16BigEndian(destination, value);
      Advance(sizeof(ushort));
    }

    public void WriteUInt32(uint value) {
      Span<byte> destination = GetSpan(sizeof(uint));
      BinaryPrimitives.WriteUInt32BigEndian(destination, value);
      Advance(sizeof(uint));
    }

    public void WriteInt32(int value) {
      Span<byte> destination = GetSpan(sizeof(int));
      BinaryPrimitives.WriteInt32BigEndian(destination, value);
      Advance(sizeof(int));
    }

    public Span<byte> GetSpan(int sizeHint = 0) => _writer.GetSpan(sizeHint);
    public Memory<byte> GetMemory(int sizeHint = 0) => _writer.GetMemory(sizeHint);
    public void Advance(int count) => _writer.Advance(count);
    public ReadOnlyMemory<byte> WrittenMemory => _writer.WrittenMemory;
    public int WrittenCount => _writer.WrittenCount;
  }

  public interface IBufferWriter {
    void Write(ReadOnlySpan<byte> source);
    void WriteByte(byte value);
    void WriteUInt16(ushort value);
    void WriteUInt32(uint value);
    void WriteInt32(int value);
  }
}
