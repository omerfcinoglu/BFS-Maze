using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

//Bu divanı lügatül şapşupayı refactor edecem ama ne zaman bilmiyom
public class MultiStrategyAnimator : MonoBehaviour
{
    [Header("Concurrency Settings")]
    public Shader overlayShader;
    public float cellSize = 1f;
    public float visitZ = -0.1f;
    public float pathZ = -0.2f;
    public float framesPerLayer = 1;

    CancellationTokenSource cts;
    Color[] threadColors;
    ConcurrentQueue<Vector2Int>[] visitQueues;
    ConcurrentQueue<Vector2Int>[] pathQueues;
    int winnerIdx = -1;

    void Start()
    {
        // Maze setup
        var maze = MazeManager.Instance.maze;
        int W = maze.GetLength(0), H = maze.GetLength(1);
        var start = new Vector2Int(1, 0);
        var goal = new Vector2Int(W - 2, H - 1);

        if (overlayShader == null)
            overlayShader = Shader.Find("Unlit/Color");

        int N = 5;
        cts = new CancellationTokenSource();
        visitQueues = new ConcurrentQueue<Vector2Int>[N];
        pathQueues = new ConcurrentQueue<Vector2Int>[N];

        // fixed colors per solver
        threadColors = new Color[]
        {
    Color.yellow,    // BFS
    Color.red,       // DFS
    new Color(0.5f,0f,0.5f), // Greedy
    Color.blue,      // RandomWalk
new Color(1f, 0.5f, 0f)
        };
        string[] names = { "BFS", "DFS", "Greedy", "RandomWalk", "A*" };
        string[] colorNames = { "Yellow", "Red", "Purple", "Blue", "Orange" };

        // init queues
        for (int i = 0; i < N; i++)
        {
            visitQueues[i] = new ConcurrentQueue<Vector2Int>();
            pathQueues[i] = new ConcurrentQueue<Vector2Int>();
        }


        for (int i = 0; i < N; i++)
            Debug.Log($"Solver {i}: {names[i]} → Color {colorNames[i]}");

        // launch solver tasks
        //Task.Run(() => RunBFS(0, maze, start, goal, cts.Token));
        //Task.Run(() => RunDFS(1, maze, start, goal, cts.Token));
        //Task.Run(() => RunGreedy(2, maze, start, goal, cts.Token));
        //Task.Run(() => RunRandomWalk(3, maze, start, goal, cts.Token));
        //Task.Run(() => RunAStar(4, maze, start, goal, cts.Token));
        // start animation coroutines
        for (int i = 0; i < N; i++)
            StartCoroutine(Animate(i));

        // report winner when set
        StartCoroutine(ReportWinner(maze));
    }

    IEnumerator ReportWinner(int[,] maze)
    {
        // wait until one solver finishes
        while (winnerIdx < 0)
            yield return null;

        string[] names = { "BFS", "DFS", "Greedy", "RandomWalk" };
        string msg = $"🎉 WINNER: Solver {winnerIdx} ({names[winnerIdx]}) 🎉";
        Debug.Log(msg);

        // create 3D text above the maze
        var textGO = new GameObject("WinnerText");
        var text = textGO.AddComponent<TextMesh>();
        text.text = msg;
        text.characterSize = 0.25f;
        text.color = Color.white;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        int W = maze.GetLength(0), H = maze.GetLength(1);
        textGO.transform.position = new Vector3(0, H / 2f + 1f, -0.5f);
    }

    // 1) BFS
    void RunBFS(int idx, int[,] maze, Vector2Int s, Vector2Int g, CancellationToken tok)
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
                visitQueues[idx].Enqueue(cell);
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
            // mark winner
            if (winnerIdx < 0) winnerIdx = idx;
            // reconstruct path
            var path = new List<Vector2Int>();
            var cur = g;
            while (cur != s)
            {
                path.Add(cur);
                cur = parent[cur];
            }
            path.Add(s);
            path.Reverse();
            foreach (var c in path)
                pathQueues[idx].Enqueue(c);
            cts.Cancel();
        }
    }

    // 2) DFS
    void RunDFS(int idx, int[,] maze, Vector2Int s, Vector2Int g, CancellationToken tok)
    {
        int W = maze.GetLength(0), H = maze.GetLength(1);
        var visited = new bool[W, H];
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var stack = new Stack<Vector2Int>();
        stack.Push(s); visited[s.x, s.y] = true;
        var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        bool reached = false;
        while (stack.Count > 0 && !tok.IsCancellationRequested)
        {
            var cell = stack.Pop();
            visitQueues[idx].Enqueue(cell);
            if (cell == g) { reached = true; break; }

            var rd = dirs.OrderBy(_ => Guid.NewGuid()).ToArray();
            foreach (var d in rd)
            {
                var np = cell + d;
                if (np.x < 0 || np.x >= W || np.y < 0 || np.y >= H) continue;
                if (maze[np.x, np.y] == 1 || visited[np.x, np.y]) continue;
                visited[np.x, np.y] = true;
                parent[np] = cell;
                stack.Push(np);
            }
        }

        if (reached)
        {
            if (winnerIdx < 0) winnerIdx = idx;
            var path = new List<Vector2Int>();
            var cur = g;
            while (cur != s)
            {
                path.Add(cur);
                cur = parent[cur];
            }
            path.Add(s);
            path.Reverse();
            foreach (var c in path)
                pathQueues[idx].Enqueue(c);
            cts.Cancel();
        }
    }

    // 3) Greedy Best-First
    void RunGreedy(int idx, int[,] maze, Vector2Int s, Vector2Int g, CancellationToken tok)
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
            visitQueues[idx].Enqueue(cell);
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
            if (winnerIdx < 0) winnerIdx = idx;
            var path = new List<Vector2Int>();
            var cur = g;
            while (cur != s)
            {
                path.Add(cur);
                cur = parent[cur];
            }
            path.Add(s);
            path.Reverse();
            foreach (var c in path)
                pathQueues[idx].Enqueue(c);
            cts.Cancel();
        }
    }

    // 4) Random-Walk + Backtrack
    void RunRandomWalk(int idx, int[,] maze, Vector2Int s, Vector2Int g, CancellationToken tok)
    {
        int W = maze.GetLength(0), H = maze.GetLength(1);
        var visited = new bool[W, H];
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var stack = new Stack<Vector2Int>();
        stack.Push(s); visited[s.x, s.y] = true;
        var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        var rnd = new System.Random(idx * 999);

        bool reached = false;
        while (stack.Count > 0 && !reached && !tok.IsCancellationRequested)
        {
            var cell = stack.Peek();
            visitQueues[idx].Enqueue(cell);
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
            if (winnerIdx < 0) winnerIdx = idx;
            var path = new List<Vector2Int>();
            var cur = g;
            while (cur != s)
            {
                path.Add(cur);
                cur = parent[cur];
            }
            path.Add(s);
            path.Reverse();
            foreach (var c in path)
                pathQueues[idx].Enqueue(c);
            cts.Cancel();
        }
    }

    // 5) A* Search (f = g + h, g maliyeti, cell = Item3)
    void RunAStar(int idx, int[,] maze, Vector2Int s, Vector2Int g, CancellationToken tok)
    {
        int W = maze.GetLength(0), H = maze.GetLength(1);
        var visited = new bool[W, H];
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [s] = 0 };

        // open set: (f, g, cell)
        var open = new SortedSet<(int, int, Vector2Int)>(
            Comparer<(int, int, Vector2Int)>.Create((a, b) =>
            {
                // önce f karşılaştır
                int diff = a.Item1 - b.Item1;
                if (diff != 0) return diff;
                // sonra g karşılaştır
                diff = a.Item2 - b.Item2;
                if (diff != 0) return diff;
                // tie-break: x sonra y
                diff = a.Item3.x - b.Item3.x;
                if (diff != 0) return diff;
                return a.Item3.y - b.Item3.y;
            })
        );

        // başlangıç
        visited[s.x, s.y] = true;
        int h0 = Mathf.Abs(s.x - g.x) + Mathf.Abs(s.y - g.y);
        open.Add((h0 + 0, 0, s));

        var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        bool reached = false;

        while (open.Count > 0 && !reached && !tok.IsCancellationRequested)
        {
            var top = open.Min; open.Remove(top);
            var cell = top.Item3;
            visitQueues[idx].Enqueue(cell);

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
            if (winnerIdx < 0) winnerIdx = idx;
            var path = new List<Vector2Int>();
            var cur = g;
            while (cur != s)
            {
                path.Add(cur);
                cur = parent[cur];
            }
            path.Add(s);
            path.Reverse();
            foreach (var c in path)
                pathQueues[idx].Enqueue(c);
            cts.Cancel();
        }
    }

    IEnumerator Animate(int idx)
    {
        var parentGO = new GameObject($"Solver{idx}");
        parentGO.transform.SetParent(transform, false);

        const int maxPerFrame = 50;  // draw up to 50 cells each frame

        // 1) Draw visits in batches of maxPerFrame
        while (!cts.IsCancellationRequested)
        {
            int drawn = 0;
            while (drawn < maxPerFrame && visitQueues[idx].TryDequeue(out var cell))
            {
                CreateQuad(parentGO.transform, cell, threadColors[idx], visitZ);
                drawn++;
            }

            // if no more to draw right now, or we've hit our per-frame limit, yield
            if (drawn == 0 || drawn >= maxPerFrame)
                yield return null;
        }

        // 2) Once cancelled, only the winner draws its path—also in one batch
        if (idx == winnerIdx)
        {
            var full = new List<Vector2Int>();
            while (pathQueues[idx].TryDequeue(out var c))
                full.Add(c);

            // draw entire path at once with no per-cell yields
            foreach (var cell in full)
                CreateQuad(parentGO.transform, cell, Color.green, pathZ);
        }

        // Done—coroutine exits, no more yields
    }

    void CreateQuad(Transform parent, Vector2Int cell, Color col, float z)
    {
        int[,] maze = MazeManager.Instance.maze;
        int W = maze.GetLength(0), H = maze.GetLength(1);
        int halfW = W / 2, halfH = H / 2;
        float px = (cell.x - halfW + 0.5f) * cellSize,
              py = (cell.y - halfH + 0.5f) * cellSize;

        var go = new GameObject("Q", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        var mf = go.GetComponent<MeshFilter>();
        var mr = go.GetComponent<MeshRenderer>();
        mr.material = new Material(overlayShader) { color = col };
        mr.material.renderQueue = 4000;

        var verts = new List<Vector3>{
            new Vector3(px-cellSize/2,py-cellSize/2,z),
            new Vector3(px+cellSize/2,py-cellSize/2,z),
            new Vector3(px+cellSize/2,py+cellSize/2,z),
            new Vector3(px-cellSize/2,py+cellSize/2,z),
        };
        var tris = new List<int> { 0, 2, 1, 0, 3, 2 };

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mf.mesh = mesh;
    }

    void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
