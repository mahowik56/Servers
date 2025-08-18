using System;
using System.Collections.Generic;
using System.Threading;
using UTanksServer.Database;
using UTanksServer.Network.Simple.Net.Client;
using UTanksServer.Network.Simple.Net.InternalEvents;
using UTanksServer.ECS.Types.Battle;
using UTanksServer.Network.NetworkEvents.FastGameEvents;
using System.Threading;
using UTanksServer.Network.Simple.Net.Client;
using UTanksServer.Network.Simple.Net.InternalEvents;
using UTanksServer.Database;

namespace UTanksServer.Services
{
    public class LoadBot
    {
        readonly string mapName;
        readonly Random random = new Random();

        Client client;
        Timer pingTimer;
        Timer moveTimer;
        Timer shootTimer;

        public LoadBot(string mapName)
        {
            this.mapName = mapName;
            client = new Client("127.0.0.1", Networking.Config["Port"].AsInt, OnConnected, () => { });
            client.Connect();
        }

        void OnConnected()
        {
            pingTimer = new Timer(_ =>
            {
                if (client.Connected)
                    client.emit(new HeartBeat { id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            }, null, 0, 1000);

            moveTimer = new Timer(_ =>
            {
                if (!client.Connected) return;
                var move = new RawMovementEvent
                {
                    packetId = random.Next(),
                    PlayerEntityId = random.Next(),
                    position = new Vector3S { x = NextFloat(), y = 0f, z = NextFloat() },
                    velocity = new Vector3S { x = NextFloat(), y = 0f, z = NextFloat() },
                    angularVelocity = new Vector3S { x = 0f, y = NextFloat(), z = 0f },
                    rotation = new QuaternionS { x = 0f, y = 0f, z = 0f, w = 1f },
                    turretRotation = new QuaternionS { x = 0f, y = 0f, z = 0f, w = 1f },
                    WeaponRotation = NextFloat(),
                    TankMoveControl = NextFloat(),
                    TankTurnControl = NextFloat(),
                    WeaponRotationControl = NextFloat(),
                    ClientTime = Environment.TickCount
                };
                client.emit(move);
            }, null, 0, 200);

            shootTimer = new Timer(_ =>
            {
                if (!client.Connected) return;
                var start = new RawStartShootingEvent
                {
                    packetId = random.Next(),
                    PlayerEntityId = random.Next(),
                    ClientTime = Environment.TickCount
                };
                var shot = new RawShotEvent
                {
                    packetId = random.Next(),
                    PlayerEntityId = start.PlayerEntityId,
                    StartGlobalPosition = new Vector3S { x = NextFloat(), y = NextFloat(), z = NextFloat() },
                    StartGlobalRotation = new QuaternionS { x = NextFloat(), y = NextFloat(), z = NextFloat(), w = NextFloat() },
                    MoveDirectionNormalized = new Vector3S { x = NextFloat(), y = NextFloat(), z = NextFloat() },
                    hitList = new Dictionary<long, Vector3S>(),
                    hitDistanceList = new Dictionary<long, float>(),
                    hitLocalDistanceList = new Dictionary<long, float>(),
                    ClientTime = Environment.TickCount
                };
                var end = new RawEndShootingEvent
                {
                    packetId = random.Next(),
                    PlayerEntityId = start.PlayerEntityId,
                    ClientTime = Environment.TickCount
                };
                client.emit(start);
                client.emit(shot);
                client.emit(end);
            }, null, 500, 500);
        }

        float NextFloat() => (float)(random.NextDouble() * 2 - 1);

        public void Stop()
        {
            pingTimer?.Dispose();
            moveTimer?.Dispose();
            shootTimer?.Dispose();
        Client client;
        Timer spamTimer;

        public LoadBot()
        {
            client = new Client("127.0.0.1", Networking.Config["Port"].AsInt,
                () => { }, () => { });
            client.Connect();
            spamTimer = new Timer(_ =>
            {
                if (client.Connected)
                    client.emit(new HeartBeat() { id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            }, null, 0, 100);
        }

        public void Stop()
        {
            spamTimer?.Dispose();
            client.Disconnect();
        }
    }
}
