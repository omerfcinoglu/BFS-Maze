// MazeGenerator.cs
using System.Collections.Generic;
using UnityEngine;

public static class MazeGenerator
{
    private static readonly Vector2Int[] Directions = {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    /// <summary>
    /// Generates a (2*cx+1)x(2*cy+1) maze array: 1=wall, 0=passage,
    /// with entrance at (1,0) and exit at (W-2, H-1).
    /// </summary>
    public static int[,] Generate(int cellsX, int cellsY)
    {
        int W = cellsX * 2 + 1;
        int H = cellsY * 2 + 1;
        int[,] maze = new int[W, H];

        // 1) fill all walls
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                maze[x, y] = 1;

        // 2) carve passages via recursive backtracker
        var rng = new System.Random();
        var stack = new Stack<Vector2Int>();
        var start = new Vector2Int(1, 1);
        maze[start.x, start.y] = 0;
        stack.Push(start);

        while (stack.Count > 0)
        {
            var cell = stack.Peek();
            // shuffle directions
            for (int i = 0; i < Directions.Length; i++)
            {
                int j = rng.Next(i, Directions.Length);
                (Directions[i], Directions[j]) = (Directions[j], Directions[i]);
            }

            bool carved = false;
            foreach (var dir in Directions)
            {
                var next = cell + dir * 2;
                if (next.x > 0 && next.x < W - 1 && next.y > 0 && next.y < H - 1
                    && maze[next.x, next.y] == 1)
                {
                    // knock down wall between
                    var wall = cell + dir;
                    maze[wall.x, wall.y] = 0;
                    maze[next.x, next.y] = 0;
                    stack.Push(next);
                    carved = true;
                    break;
                }
            }
            if (!carved) stack.Pop();
        }

        // 3) open entrance and exit
        maze[1, 0] = 0;       // entrance bottom
        maze[W - 2, H - 1] = 0;     // exit top

        return maze;
    }

    /// <summary>
    /// Debug-print to Console (optional).
    /// </summary>
    public static void DebugPrintMaze(int[,] maze)
    {
        int W = maze.GetLength(0), H = maze.GetLength(1);
        string s = "";
        for (int y = H - 1; y >= 0; y--)
        {
            for (int x = 0; x < W; x++)
                s += (maze[x, y] == 1 ? "#" : ".");
            s += "\n";
        }
        Debug.Log(s);
    }
}
