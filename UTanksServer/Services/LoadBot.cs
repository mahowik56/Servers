using System;
using System.Threading;
using UTanksServer.Network.Simple.Net.Client;
using UTanksServer.Network.Simple.Net.InternalEvents;
using UTanksServer.Database;

namespace UTanksServer.Services
{
    public class LoadBot
    {
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
