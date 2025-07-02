using MessagePack;
using UTanksServer;
using UTanksServer.Network.Simple.Net;

namespace UTanksServer.Network.NetworkEvents.PlayerAuth
{
    [MessagePackObject]
    public struct GetUserViaUsername : INetSerializable
    {
        [Key(0)]
        public int packetId;
        [Key(1)]
        public string Username;

        public void Serialize(NetWriter writer)
        {
            var bytes = MessagePackSerializer.Serialize(this,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
            writer.buffer.AddRange(bytes);
        }

        public void Deserialize(NetReader reader)
        {
            var bytes = reader.ReadRemainingBytes();
            this = MessagePackSerializer.Deserialize<GetUserViaUsername>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
        }
    }
}
