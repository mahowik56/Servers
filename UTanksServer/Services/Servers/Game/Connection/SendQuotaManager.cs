using System;
using System.Threading;

namespace UTanksServer.Services.Servers.Game.Connection {
  public sealed class SendQuotaManager {
    private readonly int _globalQueueLimitBytes;
    private readonly int _criticalReserveBytes;
    private int _globalInFlightBytes;

    public SendQuotaManager(int globalQueueLimitBytes, double criticalReserveRatio = 0.1) {
      _globalQueueLimitBytes = Math.Max(0, globalQueueLimitBytes);
      _criticalReserveBytes = (int) Math.Max(0, globalQueueLimitBytes * criticalReserveRatio);
    }

    public bool TryReserve(CommandPriority priority, int bytes, out int ticket) {
      ticket = 0;
      if(bytes <= 0) {
        return true;
      }

      while(true) {
        int current = Volatile.Read(ref _globalInFlightBytes);
        int limit = priority == CommandPriority.Critical ? _globalQueueLimitBytes : _globalQueueLimitBytes - _criticalReserveBytes;
        if(limit <= 0 || current + bytes > limit) {
          return false;
        }

        if(Interlocked.CompareExchange(ref _globalInFlightBytes, current + bytes, current) == current) {
          ticket = bytes;
          return true;
        }
      }
    }

    public void Release(int ticket) {
      if(ticket <= 0) {
        return;
      }

      Interlocked.Add(ref _globalInFlightBytes, -ticket);
    }
  }
}
