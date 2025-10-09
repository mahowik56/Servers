using System;
using System.Buffers.Binary;

namespace UTanksServer.Core.Protocol {
  public struct BufferReader {
    private ReadOnlyMemory<byte> _memory;
    private int _position;

    public BufferReader(ReadOnlyMemory<byte> memory) {
      _memory = memory;
      _position = 0;
    }

    public ReadOnlySpan<byte> RemainingSpan => _memory.Span.Slice(_position);
    public int Remaining => _memory.Length - _position;

    public bool TryReadBytes(int length, out ReadOnlyMemory<byte> value) {
      if(_position + length > _memory.Length) {
        value = default;
        return false;
      }

      value = _memory.Slice(_position, length);
      _position += length;
      return true;
    }

    public bool TryReadByte(out byte value) {
      if(_position >= _memory.Length) {
        value = default;
        return false;
      }

      value = _memory.Span[_position++];
      return true;
    }

    public bool TryReadUInt16(out ushort value) {
      if(_position + sizeof(ushort) > _memory.Length) {
        value = default;
        return false;
      }

      value = BinaryPrimitives.ReadUInt16BigEndian(_memory.Span.Slice(_position, sizeof(ushort)));
      _position += sizeof(ushort);
      return true;
    }

    public bool TryReadUInt32(out uint value) {
      if(_position + sizeof(uint) > _memory.Length) {
        value = default;
        return false;
      }

      value = BinaryPrimitives.ReadUInt32BigEndian(_memory.Span.Slice(_position, sizeof(uint)));
      _position += sizeof(uint);
      return true;
    }

    public bool TryReadInt32(out int value) {
      if(_position + sizeof(int) > _memory.Length) {
        value = default;
        return false;
      }

      value = BinaryPrimitives.ReadInt32BigEndian(_memory.Span.Slice(_position, sizeof(int)));
      _position += sizeof(int);
      return true;
    }
  }
}
