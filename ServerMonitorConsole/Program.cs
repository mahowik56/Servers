using System;

namespace ServerMonitorConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Server Monitor";
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                var parts = line.Split('|');
                Console.Clear();
                if (parts.Length >= 11)
                {
                    Console.WriteLine($"CPU Load: {parts[0]}%");
                    Console.WriteLine($"TPS: {parts[1]}");
                    Console.WriteLine($"Average Ping: {parts[2]} ms");
                    Console.WriteLine($"CCU: {parts[3]} (Peak {parts[4]})");
                    Console.WriteLine($"Packets/Client: {parts[5]}");
                    Console.WriteLine($"Avg Packet Size: {parts[6]} B");
                    Console.WriteLine($"DB R/W per player: {parts[7]}/{parts[8]}");
                    Console.WriteLine($"Rooms: {parts[9]} (Max Players: {parts[10]})");
                }
                else
                {
                    Console.WriteLine(line);
                }
            }
        }
    }
}
