using System;
using System.Collections.Generic;

namespace UTanksServer.Services
{
    public class BotMatch
    {
        static readonly Random rng = new Random();
        readonly List<LoadBot> bots = new List<LoadBot>();

        public string MapName { get; }

        public BotMatch(int botCount)
        {
            MapName = $"Map_{rng.Next(1000, 9999)}";
            Console.WriteLine($"[BotMatch] Creating map {MapName} for {botCount} bots");
            for (int i = 0; i < botCount; i++)
                bots.Add(new LoadBot(MapName));
        }

        public int BotCount => bots.Count;

        public void Stop()
        {
            foreach (var b in bots)
                b.Stop();
            bots.Clear();
        }
    }
}
