using MessagePack;
using UTanksServer.Network.Simple.Net;

namespace UTanksServer.Network.NetworkEvents.Communications {
    [MessagePackObject]
    public struct UserLoggedInEvent : INetSerializable {
        [Key(0)] public long uid;
        [Key(1)] public string Username;
        [Key(2)] public long entityId;
        [Key(3)] public long serverTime;

        public void Serialize(NetWriter writer)
        {
            var bytes = MessagePackSerializer.Serialize(this,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
            writer.buffer.AddRange(bytes);
        }

        public void Deserialize(NetReader reader)
        {
            var bytes = reader.buffer.ToArray().SubArray(reader.readPos, reader.buffer.Count - reader.readPos);
            this = MessagePackSerializer.Deserialize<UserLoggedInEvent>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
            reader.readPos = reader.buffer.Count;
        }
    }
}
