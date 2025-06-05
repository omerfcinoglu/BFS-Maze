using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class BfsSolver : IPathSolver
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
            var q = new Queue<Vector2Int>();
            q.Enqueue(s); visited[s.x, s.y] = true;
            var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            bool reached = false;
            while (q.Count > 0 && !tok.IsCancellationRequested)
            {
                int layerCount = q.Count;
                var next = new List<Vector2Int>();
                for (int i = 0; i < layerCount; i++)
                {
                    var cell = q.Dequeue();
                    visits.Enqueue(cell);
                    if (cell == g) { reached = true; break; }
                    foreach (var d in dirs)
                    {
                        var np = cell + d;
                        if (np.x < 0 || np.x >= W || np.y < 0 || np.y >= H) continue;
                        if (maze[np.x, np.y] == 1 || visited[np.x, np.y]) continue;
                        visited[np.x, np.y] = true;
                        parent[np] = cell;
                        next.Add(np);
                    }
                }
                if (reached) break;
                foreach (var c in next) q.Enqueue(c);
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
