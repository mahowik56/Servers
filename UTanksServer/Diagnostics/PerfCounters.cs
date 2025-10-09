using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace UTanksServer.Diagnostics {
  public static class PerfCounters {
    private static readonly ConcurrentDictionary<string, Counter> Counters = new ConcurrentDictionary<string, Counter>();

    public static IDisposable TrackDuration(string name) {
      Counter counter = Counters.GetOrAdd(name, _ => new Counter());
      return counter.Track();
    }

    public static void Increment(string name, double value = 1.0) {
      Counter counter = Counters.GetOrAdd(name, _ => new Counter());
      counter.Add(value);
    }

    private sealed class Counter {
      private double _value;
      private readonly object _sync = new object();

      public IDisposable Track() {
        Stopwatch sw = Stopwatch.StartNew();
        return new Scope(sw, this);
      }

      public void Add(double value) {
        lock(_sync) {
          _value += value;
        }
      }

      private void Observe(TimeSpan duration) {
        lock(_sync) {
          _value = duration.TotalMilliseconds;
        }
      }

      private sealed class Scope : IDisposable {
        private readonly Stopwatch _stopwatch;
        private readonly Counter _counter;
        private bool _disposed;

        public Scope(Stopwatch stopwatch, Counter counter) {
          _stopwatch = stopwatch;
          _counter = counter;
        }

        public void Dispose() {
          if(_disposed) {
            return;
          }

          _disposed = true;
          _stopwatch.Stop();
          _counter.Observe(_stopwatch.Elapsed);
        }
      }
    }
  }
}
