using System.Collections.Concurrent;

namespace UTanksServer.Services.Servers.Game.World {
  public sealed class SnapshotCache {
    private readonly ConcurrentDictionary<Player, WorldSnapshot> _snapshots = new ConcurrentDictionary<Player, WorldSnapshot>();

    public void Store(Player player, WorldSnapshot snapshot) {
      _snapshots[player] = snapshot;
    }

    public WorldSnapshot Get(Player player) {
      _snapshots.TryGetValue(player, out WorldSnapshot snapshot);
      return snapshot;
    }
  }
}
