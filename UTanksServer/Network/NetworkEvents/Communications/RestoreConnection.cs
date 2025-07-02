using MessagePack;
using UTanksServer;
using UTanksServer.Network.Simple.Net;

namespace UTanksServer.Network.NetworkEvents.Communications
{
    [MessagePackObject]
    public struct RestoreConnection : INetSerializable
    {
        [Key(0)]
        public long uid;

        public void Serialize(NetWriter writer)
        {
            var bytes = MessagePackSerializer.Serialize(this,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
            writer.buffer.AddRange(bytes);
        }

        public void Deserialize(NetReader reader)
        {
            var bytes = reader.ReadRemainingBytes();
            this = MessagePackSerializer.Deserialize<RestoreConnection>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
        }
    }
}
