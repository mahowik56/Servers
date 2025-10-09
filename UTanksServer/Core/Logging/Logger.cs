using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UTanksServer.Core.Logging {
  internal static class Logger {
    private static readonly Channel<LogMessage> Channel = System.Threading.Channels.Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions {
      AllowSynchronousContinuations = false,
      SingleReader = true,
      SingleWriter = false
    });

    private static readonly Task ProcessingTask;

    static Logger() {
      ProcessingTask = Task.Run(ProcessQueueAsync);
    }

    public static void Log(object content) => Enqueue("INFO", ConsoleColor.Gray, content);
    public static void Debug(object content) => Enqueue("DEBUG", ConsoleColor.DarkGreen, content);
    public static void Trace(object content) => Enqueue("TRACE", ConsoleColor.DarkGray, content, sample: true);
    public static void Warn(object content) => Enqueue("WARN", ConsoleColor.DarkYellow, content);
    public static void Error(object content) => Enqueue("ERROR", ConsoleColor.Red, content);

    private static void Enqueue(string type, ConsoleColor color, object content, bool sample = false) {
      if(sample && !Sampler.ShouldWrite(type)) {
        return;
      }

      Channel.Writer.TryWrite(new LogMessage(type, color, content, DateTime.UtcNow));
    }

    private static async Task ProcessQueueAsync() {
      await foreach(LogMessage message in Channel.Reader.ReadAllAsync()) {
        Write(message);
      }
    }

    private static void Write(LogMessage message) {
      ConsoleColor original = Console.ForegroundColor;
      try {
        Console.ForegroundColor = message.Color;
        Console.WriteLine($"[{message.Timestamp:O}, {message.Type}] {message.Content}");
      } finally {
        Console.ForegroundColor = original;
      }
    }

    private readonly record struct LogMessage(string Type, ConsoleColor Color, object Content, DateTime Timestamp);

    private static class Sampler {
      private static readonly ConcurrentDictionary<string, SamplerState> States = new ConcurrentDictionary<string, SamplerState>();

      public static bool ShouldWrite(string key) {
        SamplerState state = States.GetOrAdd(key, _ => new SamplerState(100, TimeSpan.FromSeconds(1)));
        return state.TryConsume(DateTime.UtcNow);
      }

      private sealed class SamplerState {
        private readonly int _limit;
        private readonly TimeSpan _period;
        private int _count;
        private DateTime _windowStart;
        private readonly object _sync = new object();

        public SamplerState(int limit, TimeSpan period) {
          _limit = limit;
          _period = period;
          _windowStart = DateTime.UtcNow;
        }

        public bool TryConsume(DateTime now) {
          lock(_sync) {
            if(now - _windowStart > _period) {
              _windowStart = now;
              _count = 0;
            }

            if(_count >= _limit) {
              return false;
            }

            _count++;
            return true;
          }
        }
      }
    }
  }
}
