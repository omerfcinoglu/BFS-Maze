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
    List<IPathSolver> solvers;

    void Start()
    {
        // Maze setup
        var maze = MazeManager.Instance.maze;
        int W = maze.GetLength(0), H = maze.GetLength(1);
        var start = new Vector2Int(1, 0);
        var goal = new Vector2Int(W - 2, H - 1);

        if (overlayShader == null)
            overlayShader = Shader.Find("Unlit/Color");

        solvers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IPathSolver).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => (IPathSolver)Activator.CreateInstance(t))
            .ToList();

        int N = solvers.Count;
        cts = new CancellationTokenSource();
        visitQueues = new ConcurrentQueue<Vector2Int>[N];
        pathQueues = new ConcurrentQueue<Vector2Int>[N];

        Color[] baseColors =
        {
            Color.yellow,
            Color.red,
            new Color(0.5f,0f,0.5f),
            Color.blue,
            new Color(1f, 0.5f, 0f)
        };
        threadColors = new Color[N];
        for (int i = 0; i < N; i++)
            threadColors[i] = i < baseColors.Length ? baseColors[i] : UnityEngine.Random.ColorHSV();

        string[] names = solvers.Select(s => s.GetType().Name).ToArray();
        string[] colorNames = threadColors.Select(c => ColorUtility.ToHtmlStringRGB(c)).ToArray();

        // init queues
        for (int i = 0; i < N; i++)
        {
            visitQueues[i] = new ConcurrentQueue<Vector2Int>();
            pathQueues[i] = new ConcurrentQueue<Vector2Int>();
        }


        for (int i = 0; i < N; i++)
            Debug.Log($"Solver {i}: {names[i]} → Color {colorNames[i]}");

        // launch solver tasks
        for (int i = 0; i < N; i++)
        {
            int idx = i;
            solvers[idx]
                .Solve(maze, start, goal, cts.Token, visitQueues[idx], pathQueues[idx])
                .ContinueWith(_ =>
                {
                    if (pathQueues[idx].Count > 0 && winnerIdx < 0)
                    {
                        winnerIdx = idx;
                        cts.Cancel();
                    }
                });
        }
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

        string[] names = solvers.Select(s => s.GetType().Name).ToArray();
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
