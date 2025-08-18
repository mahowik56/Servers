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
                if (parts.Length >= 3)
                {
                    Console.WriteLine($"CPU Load: {parts[0]}%");
                    Console.WriteLine($"TPS: {parts[1]}");
                    Console.WriteLine($"Average Ping: {parts[2]} ms");
                }
                else
                {
                    Console.WriteLine(line);
                }
            }
        }
    }
}
