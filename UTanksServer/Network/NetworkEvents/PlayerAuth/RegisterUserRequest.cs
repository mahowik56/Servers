using UTanksServer.Network.Simple.Net;
using MessagePack;

namespace UTanksServer.Network.NetworkEvents.PlayerAuth
{
    [MessagePackObject]
    public struct RegisterUserRequest : INetSerializable
    {
        [Key(0)] public int packetId;
        [Key(1)] public string Username;
        [Key(2)] public string Password;
        [Key(3)] public string Email;
        [Key(4)] public string HardwareId;
        [Key(5)] public string HardwareToken;
        [Key(6)] public string CountryCode;
        [Key(7)] public bool subscribed;
        [IgnoreMember]
        public string captchaResultHash;

        public void Serialize(NetWriter writer)
        {
            byte[] data = MessagePackSerializer.Serialize(this,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
            writer.Push((long)data.Length);
            writer.buffer.AddRange(data);
        }

        public void Deserialize(NetReader reader)
        {
            long length = reader.ReadInt64();
            byte[] bytes = reader.buffer.GetRange(reader.readPos, (int)length).ToArray();
            reader.readPos += (int)length;
            var obj = MessagePackSerializer.Deserialize<RegisterUserRequest>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
            this = obj;
        }
    }
}
