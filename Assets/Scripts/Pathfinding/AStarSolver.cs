using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AStarSolver : IPathSolver
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
            var gScore = new Dictionary<Vector2Int, int> { [s] = 0 };
            var open = new SortedSet<(int, int, Vector2Int)>(
                Comparer<(int, int, Vector2Int)>.Create((a, b) =>
                {
                    int diff = a.Item1 - b.Item1; if (diff != 0) return diff;
                    diff = a.Item2 - b.Item2; if (diff != 0) return diff;
                    diff = a.Item3.x - b.Item3.x; if (diff != 0) return diff;
                    return a.Item3.y - b.Item3.y;
                })
            );
            visited[s.x, s.y] = true;
            int h0 = Mathf.Abs(s.x - g.x) + Mathf.Abs(s.y - g.y);
            open.Add((h0, 0, s));
            var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            bool reached = false;
            while (open.Count > 0 && !reached && !tok.IsCancellationRequested)
            {
                var top = open.Min; open.Remove(top);
                var cell = top.Item3;
                visits.Enqueue(cell);
                if (cell == g) { reached = true; break; }
                foreach (var d in dirs)
                {
                    var np = cell + d;
                    if (np.x < 0 || np.x >= W || np.y < 0 || np.y >= H) continue;
                    if (maze[np.x, np.y] == 1) continue;
                    int tentativeG = top.Item2 + 1;
                    if (!gScore.TryGetValue(np, out var prevG) || tentativeG < prevG)
                    {
                        gScore[np] = tentativeG;
                        parent[np] = cell;
                        int h = Mathf.Abs(np.x - g.x) + Mathf.Abs(np.y - g.y);
                        open.Add((tentativeG + h, tentativeG, np));
                        visited[np.x, np.y] = true;
                    }
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
