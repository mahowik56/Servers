using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using UTanksServer.Database;

namespace UTanksServer.Services
{
    public static class ServerMonitor
    {
        static Thread monitorThread;
        static Process monitorProcess;
        static StreamWriter monitorInput;
        static int tickCount;
        public static void Start()
        {
            StartMonitorConsole();
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

                monitorInput?.WriteLine($"{cpuUsage:0.0}|{ticks}|{avgPing:0.0}");
            }
        }

        static void StartMonitorConsole()
        {
            try
            {
                var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerMonitorConsole.dll");
                if (!File.Exists(assemblyPath))
                {
                    // fallback for development path
                    assemblyPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ServerMonitorConsole", "bin", "Debug", "net5.0", "ServerMonitorConsole.dll"));
                }
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{assemblyPath}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = false
                };
                monitorProcess = Process.Start(psi);
                monitorInput = monitorProcess?.StandardInput;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start monitor console: {ex.Message}");
            }
        }
    }
}
