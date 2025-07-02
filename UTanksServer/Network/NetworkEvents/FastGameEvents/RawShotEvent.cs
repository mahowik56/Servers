using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using UTanksServer;
using UTanksServer.ECS.Types.Battle;
using UTanksServer.Network.Simple.Net;

namespace UTanksServer.Network.NetworkEvents.FastGameEvents
{
    [MessagePackObject]
    public struct RawShotEvent : INetSerializable
    {
        [Key(0)] public int packetId;
        [Key(1)] public long PlayerEntityId;
        [Key(2)] public Vector3S StartGlobalPosition;
        [Key(3)] public QuaternionS StartGlobalRotation;
        [Key(4)] public Vector3S MoveDirectionNormalized;
        [Key(5)] public Dictionary<long, Vector3S> hitList;
        [Key(6)] public Dictionary<long, float> hitDistanceList;
        [Key(7)] public Dictionary<long, float> hitLocalDistanceList;
        [Key(8)] public int ClientTime { get; set; }

        public void Serialize(NetWriter writer)
        {
            var bytes = MessagePackSerializer.Serialize(this,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
            writer.buffer.AddRange(bytes);
        }

        public void Deserialize(NetReader reader)
        {
            var bytes = reader.ReadRemainingBytes();
            this = MessagePackSerializer.Deserialize<RawShotEvent>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
        }
    }
}
