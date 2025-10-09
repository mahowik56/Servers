using System.Collections.Generic;
using System.Numerics;

namespace UTanksServer.Services.Servers.Game.World {
  public sealed class InterestManager {
    private readonly SpatialHashGrid<Player> _grid;
    private readonly float _visibilityRadius;

    public InterestManager(float cellSize, float visibilityRadius) {
      _grid = new SpatialHashGrid<Player>(cellSize);
      _visibilityRadius = visibilityRadius;
    }

    public void UpdatePlayer(Player player, long entityId, Vector3 position) {
      _grid.Set(entityId, position, player);
    }

    public void RemovePlayer(long entityId, Vector3 position) {
      _grid.Remove(entityId, position);
    }

    public IReadOnlyList<Player> Query(Vector3 position) {
      return _grid.Query(position, _visibilityRadius);
    }
  }
}
