using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace UTanksServer.Network
{
    public class UdpTransport
    {
        private readonly NetManager _manager;
        private readonly NetDataWriter _writer = new();
        private readonly List<byte[]> _pending = new();
        private readonly int _mtu;

        public UdpTransport(int port, int mtu = 1200)
        {
            _mtu = mtu;
            _manager = new NetManager(new EventListener());
            _manager.Start(port);
        }

        public void Enqueue(byte[] data)
        {
            _pending.Add(data);
        }

        public async Task Flush()
        {
            if(_pending.Count == 0) return;
            _writer.Reset();
            foreach(var packet in _pending.ToArray())
            {
                if(_writer.Length + packet.Length > _mtu)
                {
                    _manager.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
                    _writer.Reset();
                }
                _writer.Put(packet);
            }
            if(_writer.Length > 0)
                _manager.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
            _pending.Clear();
            await Task.Yield();
        }
    }

    internal class EventListener : INetEventListener
    {
        public void OnConnectionRequest(ConnectionRequest request) => request.Accept();
        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) { }
        public void OnPeerConnected(NetPeer peer) { }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }
        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    }
}
