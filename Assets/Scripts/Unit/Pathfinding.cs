using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private static readonly Vector2Int[] Directions = new[]
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool isOurBoard)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return null;

        // Simple BFS pathfinding
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var frontier = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == end) break;

            foreach (var dir in Directions)
            {
                var next = current + dir;
                if (!gm.IsInBounds(next)) continue;
                if (visited.Contains(next)) continue;

                var cell = gm.GetCell(next, isOurBoard);
                if (cell.IsOccupied && next != end) continue;

                visited.Add(next);
                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(end) && start != end)
            return new List<Vector2Int>(); // No path

        // Reconstruct path (excluding start)
        var path = new List<Vector2Int>();
        var cur = end;
        while (cur != start)
        {
            path.Add(cur);
            if (!cameFrom.TryGetValue(cur, out var prev)) break;
            cur = prev;
        }
        path.Reverse();
        return path;
    }
}
