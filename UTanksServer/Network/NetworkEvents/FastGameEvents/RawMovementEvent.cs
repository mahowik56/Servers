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
    public struct RawMovementEvent : INetSerializable
    {
        [Key(0)] public int packetId;
        [Key(1)] public long PlayerEntityId;
        [Key(2)] public Vector3S position;
        [Key(3)] public Vector3S velocity;
        [Key(4)] public Vector3S angularVelocity;
        [Key(5)] public QuaternionS rotation;
        [Key(6)] public QuaternionS turretRotation;
        [Key(7)] public float WeaponRotation { get; set; }
        [Key(8)] public float TankMoveControl { get; set; }
        [Key(9)] public float TankTurnControl { get; set; }
        [Key(10)] public float WeaponRotationControl { get; set; }
        [Key(11)] public int ClientTime { get; set; }

        public void Serialize(NetWriter writer)
        {
            var bytes = MessagePackSerializer.Serialize(this,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
            writer.buffer.AddRange(bytes);
        }

        public void Deserialize(NetReader reader)
        {
            var bytes = reader.ReadRemainingBytes();
            this = MessagePackSerializer.Deserialize<RawMovementEvent>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block));
        }
    }
}
