using System;
using System.Collections.Concurrent;

namespace UTanksServer.Services.Servers.Game.Security {
  public sealed class AntiFloodController {
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new ConcurrentDictionary<string, TokenBucket>();

    public bool TryConsume(Player player, string key, int cost, DateTime now) {
      string bucketKey = $"{player.GetHashCode()}::{key}";
      TokenBucket bucket = _buckets.GetOrAdd(bucketKey, _ => new TokenBucket(10, TimeSpan.FromSeconds(1)));
      return bucket.TryConsume(cost, now);
    }

    private sealed class TokenBucket {
      private readonly int _capacity;
      private readonly TimeSpan _refillPeriod;
      private double _tokens;
      private DateTime _lastRefill;
      private readonly object _sync = new object();

      public TokenBucket(int capacity, TimeSpan refillPeriod) {
        _capacity = capacity;
        _refillPeriod = refillPeriod;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
      }

      public bool TryConsume(int cost, DateTime now) {
        lock(_sync) {
          Refill(now);
          if(_tokens < cost) {
            return false;
          }

          _tokens -= cost;
          return true;
        }
      }

      private void Refill(DateTime now) {
        double periods = (now - _lastRefill).TotalMilliseconds / _refillPeriod.TotalMilliseconds;
        if(periods <= 0) {
          return;
        }

        _tokens = Math.Min(_capacity, _tokens + periods * _capacity);
        _lastRefill = now;
      }
    }
  }
}
