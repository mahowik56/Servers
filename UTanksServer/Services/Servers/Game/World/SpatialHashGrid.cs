using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace UTanksServer.Services.Servers.Game.World {
  public sealed class SpatialHashGrid<TValue> where TValue : class {
    private readonly ConcurrentDictionary<(int, int), ConcurrentDictionary<long, (Vector3 Position, TValue Value)>> _cells = new ConcurrentDictionary<(int, int), ConcurrentDictionary<long, (Vector3, TValue)>>();
    private readonly float _cellSize;

    public SpatialHashGrid(float cellSize) {
      _cellSize = Math.Max(1f, cellSize);
    }

    public void Set(long id, Vector3 position, TValue value) {
      (int cellX, int cellY) = GetCell(position);
      ConcurrentDictionary<long, (Vector3, TValue)> cell = _cells.GetOrAdd((cellX, cellY), _ => new ConcurrentDictionary<long, (Vector3, TValue)>());
      cell[id] = (position, value);
    }

    public void Remove(long id, Vector3 position) {
      (int cellX, int cellY) = GetCell(position);
      if(_cells.TryGetValue((cellX, cellY), out ConcurrentDictionary<long, (Vector3, TValue)> cell)) {
        cell.TryRemove(id, out _);
      }
    }

    public List<TValue> Query(Vector3 position, float radius) {
      List<TValue> results = new List<TValue>();
      int minX = (int) Math.Floor((position.X - radius) / _cellSize);
      int maxX = (int) Math.Floor((position.X + radius) / _cellSize);
      int minY = (int) Math.Floor((position.Z - radius) / _cellSize);
      int maxY = (int) Math.Floor((position.Z + radius) / _cellSize);

      float radiusSquared = radius * radius;
      for(int x = minX; x <= maxX; x++) {
        for(int y = minY; y <= maxY; y++) {
          if(!_cells.TryGetValue((x, y), out ConcurrentDictionary<long, (Vector3, TValue)> cell)) {
            continue;
          }

          foreach((Vector3 candidatePosition, TValue value) in cell.Values) {
            if(Vector3.DistanceSquared(candidatePosition, position) <= radiusSquared) {
              results.Add(value);
            }
          }
        }
      }

      return results;
    }

    private (int, int) GetCell(Vector3 position) {
      int cellX = (int) Math.Floor(position.X / _cellSize);
      int cellY = (int) Math.Floor(position.Z / _cellSize);
      return (cellX, cellY);
    }
  }
}
