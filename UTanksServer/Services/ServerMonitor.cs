using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using UTanksServer.Database;
using UTanksServer.ECS.ECSCore;
using UTanksServer.ECS.Components.Battle;
using UTanksServer.ECS.Components.Battle.BattleComponents;

namespace UTanksServer.Services
{
    public static class ServerMonitor
    {
        static Thread monitorThread;
        static Process monitorProcess;
        static StreamWriter monitorInput;
        static int tickCount;
        static long dbReads;
        static long dbWrites;
        static int peakCcu;
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

        public static void RegisterDbRead()
        {
            Interlocked.Increment(ref dbReads);
        }

        public static void RegisterDbWrite()
        {
            Interlocked.Increment(ref dbWrites);
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
                int ccu = 0;
                long packetsIn = 0, packetsOut = 0, bytesIn = 0, bytesOut = 0;
                var users = Networking.server?.users;
                if (users != null && users.Count > 0)
                {
                    ccu = users.Count;
                    peakCcu = Math.Max(peakCcu, ccu);
                    foreach (var u in users)
                    {
                        packetsIn += Interlocked.Exchange(ref u.userPackets, 0);
                        packetsOut += Interlocked.Exchange(ref u.emitPackets, 0);
                        bytesIn += Interlocked.Exchange(ref u.bytesReceived, 0);
                        bytesOut += Interlocked.Exchange(ref u.bytesSent, 0);
                    }
                    var pingValues = users.Where(u => u.Ping > 0).Select(u => (double)u.Ping);
                    if (pingValues.Any())
                        avgPing = pingValues.Average();
                }
                double avgPacketsPerClient = ccu > 0 ? (double)(packetsIn + packetsOut) / ccu : 0;
                double avgPacketSize = (packetsIn + packetsOut) > 0 ? (double)(bytesIn + bytesOut) / (packetsIn + packetsOut) : 0;
                long reads = Interlocked.Exchange(ref dbReads, 0);
                long writes = Interlocked.Exchange(ref dbWrites, 0);
                double readsPerPlayer = ccu > 0 ? (double)reads / ccu : 0;
                double writesPerPlayer = ccu > 0 ? (double)writes / ccu : 0;

                int activeRooms = 0;
                int maxPlayersRoom = 0;
                if (ManagerScope.entityManager != null)
                {
                    var battles = ManagerScope.entityManager.EntityStorage.Values.Where(e => e.HasComponent(BattleComponent.Id));
                    activeRooms = battles.Count();
                    foreach (var b in battles)
                    {
                        if (b.HasComponent(BattlePlayersComponent.Id))
                        {
                            var players = (b.GetComponent(BattlePlayersComponent.Id) as BattlePlayersComponent).players.Count;
                            if (players > maxPlayersRoom) maxPlayersRoom = players;
                        }
                    }
                }

                Console.WriteLine($"CPU:{cpuUsage:0.0}% TPS:{ticks} CCU:{ccu} Peak:{peakCcu} Pkt/Client:{avgPacketsPerClient:0.0} AvgPkt:{avgPacketSize:0.0}B DB R/W:{readsPerPlayer:0.0}/{writesPerPlayer:0.0} Rooms:{activeRooms} MaxPlayersRoom:{maxPlayersRoom}");
                monitorInput?.WriteLine($"{cpuUsage:0.0}|{ticks}|{avgPing:0.0}|{ccu}|{peakCcu}|{avgPacketsPerClient:0.0}|{avgPacketSize:0.0}|{readsPerPlayer:0.0}|{writesPerPlayer:0.0}|{activeRooms}|{maxPlayersRoom}");
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
