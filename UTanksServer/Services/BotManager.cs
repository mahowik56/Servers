using System.Collections.Generic;
using System.Linq;

namespace UTanksServer.Services
{
    public static class BotManager
    {
        static readonly List<BotMatch> matches = new List<BotMatch>();

        public static void Start(int totalBots)
        {
            Stop();
            int remaining = totalBots;
            while (remaining > 0)
            {
                int batch = remaining > 16 ? 16 : remaining;
                matches.Add(new BotMatch(batch));
                remaining -= batch;
            }
        static readonly List<LoadBot> bots = new List<LoadBot>();

        public static void Start(int count)
        {
            for (int i = 0; i < count; i++)
                bots.Add(new LoadBot());
        }

        public static void Stop()
        {
            foreach (var m in matches)
                m.Stop();
            matches.Clear();
        }

        public static int Count => matches.Sum(m => m.BotCount);
            foreach (var b in bots)
                b.Stop();
            bots.Clear();
        }

        public static int Count => bots.Count;
    }
}
