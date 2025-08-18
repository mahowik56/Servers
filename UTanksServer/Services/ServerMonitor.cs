using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UTanksServer.Database;

namespace UTanksServer.Services
{
    public static class ServerMonitor
    {
#if Windows
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();
#endif
        static Thread monitorThread;
        static int tickCount;
        public static void Start()
        {
            monitorThread = new Thread(MonitorLoop)
            {
                IsBackground = true
            };
            monitorThread.Start();
        }

        public static void RegisterTick()
        {
            Interlocked.Increment(ref tickCount);
        }

        static void MonitorLoop()
        {
#if Windows
            AllocConsole();
#endif
            var process = Process.GetCurrentProcess();
            var lastCpu = process.TotalProcessorTime;
            var lastTime = DateTime.UtcNow;
            while (true)
            {
                Thread.Sleep(1000);
                var nowCpu = process.TotalProcessorTime;
                var nowTime = DateTime.UtcNow;
                double cpuUsage = 0;
                var cpuDelta = (nowCpu - lastCpu).TotalMilliseconds;
                var timeDelta = (nowTime - lastTime).TotalMilliseconds;
                if (timeDelta > 0)
                    cpuUsage = cpuDelta / (Environment.ProcessorCount * timeDelta) * 100.0;
                lastCpu = nowCpu;
                lastTime = nowTime;

                int ticks = Interlocked.Exchange(ref tickCount, 0);
                double avgPing = 0;
                var users = Networking.server?.users;
                if (users != null && users.Count > 0)
                {
                    var pingValues = users.Where(u => u.Ping > 0).Select(u => (double)u.Ping);
                    if (pingValues.Any())
                        avgPing = pingValues.Average();
                }

                Console.Clear();
                Console.WriteLine($"CPU Load: {cpuUsage:0.0}%");
                Console.WriteLine($"TPS: {ticks}");
                Console.WriteLine($"Average Ping: {avgPing:0.0} ms");
            }
        }
    }
}
