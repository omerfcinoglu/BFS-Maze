using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GreedySolver : IPathSolver
{
    public Task Solve(int[,] maze, Vector2Int s, Vector2Int g, CancellationToken tok,
                      ConcurrentQueue<Vector2Int> visits,
                      ConcurrentQueue<Vector2Int> path)
    {
        return Task.Run(() =>
        {
            int W = maze.GetLength(0), H = maze.GetLength(1);
            var visited = new bool[W, H];
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var pq = new SortedSet<(int, Vector2Int)>(
                Comparer<(int, Vector2Int)>.Create((a, b) =>
                {
                    int diff = a.Item1 - b.Item1; if (diff != 0) return diff;
                    diff = a.Item2.x - b.Item2.x; if (diff != 0) return diff;
                    return a.Item2.y - b.Item2.y;
                })
            );
            visited[s.x, s.y] = true;
            pq.Add((Mathf.Abs(s.x - g.x) + Mathf.Abs(s.y - g.y), s));
            bool reached = false;
            var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (pq.Count > 0 && !reached && !tok.IsCancellationRequested)
            {
                var top = pq.Min; pq.Remove(top);
                var cell = top.Item2;
                visits.Enqueue(cell);
                if (cell == g) { reached = true; break; }
                foreach (var d in dirs)
                {
                    var np = cell + d;
                    if (np.x < 0 || np.x >= W || np.y < 0 || np.y >= H) continue;
                    if (maze[np.x, np.y] == 1 || visited[np.x, np.y]) continue;
                    visited[np.x, np.y] = true;
                    parent[np] = cell;
                    int h = Mathf.Abs(np.x - g.x) + Mathf.Abs(np.y - g.y);
                    pq.Add((h, np));
                }
            }
            if (reached)
            {
                var cur = g;
                var list = new List<Vector2Int>();
                while (cur != s)
                {
                    list.Add(cur);
                    cur = parent[cur];
                }
                list.Add(s);
                list.Reverse();
                foreach (var c in list) path.Enqueue(c);
            }
        }, tok);
    }
}
