using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using UTanksServer.Services.Servers.Game;

namespace UTanksServer.Core.Protocol {
  public interface ICommandCodec {
    ushort TypeId { get; }
    Type CommandType { get; }
    void Encode(ref BufferWriter writer, ICommand command);
    ICommand Decode(ref BufferReader reader, Player player);
    int EstimateSize(ICommand command);
  }

  public static class CommandCodecRegistry {
    private static readonly ConcurrentDictionary<Type, ICommandCodec> CodecsByType = new ConcurrentDictionary<Type, ICommandCodec>();
    private static readonly ConcurrentDictionary<ushort, ICommandCodec> CodecsById = new ConcurrentDictionary<ushort, ICommandCodec>();

    public static void Register(ICommandCodec codec) {
      if(!CodecsByType.TryAdd(codec.CommandType, codec)) {
        throw new InvalidOperationException($"Codec for type {codec.CommandType} already registered");
      }

      if(!CodecsById.TryAdd(codec.TypeId, codec)) {
        throw new InvalidOperationException($"Codec id {codec.TypeId} already registered");
      }
    }

    public static ICommandCodec GetByType(Type type) {
      if(CodecsByType.TryGetValue(type, out ICommandCodec codec)) {
        return codec;
      }

      throw new KeyNotFoundException($"Codec for {type} is not registered");
    }

    public static ICommandCodec GetById(ushort id) {
      if(CodecsById.TryGetValue(id, out ICommandCodec codec)) {
        return codec;
      }

      throw new KeyNotFoundException($"Codec with id {id} is not registered");
    }
  }
}
