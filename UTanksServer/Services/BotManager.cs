using System.Collections.Generic;

namespace UTanksServer.Services
{
    public static class BotManager
    {
        private static readonly List<LoadBot> bots = new();

        public static void Start(int count)
        {
            Stop();
            for (int i = 0; i < count; i++)
                bots.Add(new LoadBot());
        }

        public static void Stop()
        {
            foreach (var b in bots)
                b.Stop();
            bots.Clear();
        }

        public static int Count => bots.Count;
    }
}
