using System.Collections.Generic;

namespace UTanksServer.Services
{
    public static class BotManager
    {
        static readonly List<LoadBot> bots = new List<LoadBot>();

        public static void Start(int count)
        {
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
