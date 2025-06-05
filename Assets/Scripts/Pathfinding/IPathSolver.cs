using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public interface IPathSolver
{
    Task Solve(int[,] maze, Vector2Int start, Vector2Int goal,
               CancellationToken token,
               ConcurrentQueue<Vector2Int> visits,
               ConcurrentQueue<Vector2Int> path);
}
