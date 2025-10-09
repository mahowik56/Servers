using System.Collections.Generic;

namespace UTanksServer.Services.Servers.Game.World {
  public sealed class WorldSnapshot {
    public long Tick { get; init; }
    public Dictionary<long, EntityState> Entities { get; } = new Dictionary<long, EntityState>();
  }

  public sealed class EntityState {
    public long EntityId { get; init; }
    public byte[] Payload { get; init; }
  }
}
