using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RandomWalkSolver : IPathSolver
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
            var stack = new Stack<Vector2Int>();
            stack.Push(s); visited[s.x, s.y] = true;
            var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            var rnd = new System.Random(999);
            bool reached = false;
            while (stack.Count > 0 && !reached && !tok.IsCancellationRequested)
            {
                var cell = stack.Peek();
                visits.Enqueue(cell);
                if (cell == g) { reached = true; break; }
                var rd = dirs.OrderBy(_ => rnd.Next()).ToArray();
                bool moved = false;
                foreach (var d in rd)
                {
                    var np = cell + d;
                    if (np.x < 0 || np.x >= W || np.y < 0 || np.y >= H) continue;
                    if (maze[np.x, np.y] == 1 || visited[np.x, np.y]) continue;
                    visited[np.x, np.y] = true;
                    parent[np] = cell;
                    stack.Push(np);
                    moved = true;
                    break;
                }
                if (!moved) stack.Pop();
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
