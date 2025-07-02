using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using UTanksServer;
using UTanksServer.Network.Simple.Net;

namespace Assets.ClientCore.CoreImpl.Network.NetworkEvents.GameData
{
    //this events automatically resending into ECSEventManager, in json store all needed data for ecs event
    [MessagePackObject]
    public struct GameDataEvent : INetSerializable
    {
        [Key(0)] public int packetId;
        [Key(1)] public long typeId;
        [Key(2)] public string jsonData;

        public void Serialize(NetWriter writer)
        {
            var bytes = MessagePackSerializer.Serialize(this,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
            writer.buffer.AddRange(bytes);
        }

        public void Deserialize(NetReader reader)
        {
            var bytes = reader.ReadRemainingBytes();
            this = MessagePackSerializer.Deserialize<GameDataEvent>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
        }
    }
}
