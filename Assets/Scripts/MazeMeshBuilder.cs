using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MazeMeshBuilder : MonoBehaviour
{
    [Header("Cell Dimensions")]
    private float cellSize = 1f;

    private Material floorMaterial;
    private Material wallMaterial;

    void Awake()
    {
        // Create unlit color materials at runtime
        Shader shader = Shader.Find("Unlit/Color");
        floorMaterial = new Material(shader) { color = Color.white };
        wallMaterial = new Material(shader) { color = Color.black };
    }

    void Start()
    {
        // 1) generate the raw grid
        var maze = MazeManager.Instance.maze;
        int W = maze.GetLength(0), H = maze.GetLength(1);
        cellSize = MazeManager.Instance.cellSize;

        // 2) build two Meshes: floor and walls
        Mesh floorMesh = BuildQuadMesh(maze, W, H, targetValue: 0);
        Mesh wallMesh = BuildQuadMesh(maze, W, H, targetValue: 1);

        // 3) assign floor mesh + material
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        mf.mesh = floorMesh;
        mr.material = floorMaterial;

        // 4) create child for walls
        var wallGO = new GameObject("MazeWalls", typeof(MeshFilter), typeof(MeshRenderer));
        wallGO.transform.SetParent(transform, false);
        wallGO.GetComponent<MeshFilter>().mesh = wallMesh;
        wallGO.GetComponent<MeshRenderer>().material = wallMaterial;
    }

    Mesh BuildQuadMesh(int[,] maze, int W, int H, int targetValue)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        int halfW = W / 2, halfH = H / 2;
        int idx = 0;

        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                if (maze[x, y] != targetValue) continue;

                float px = (x - halfW + 0.5f) * cellSize;
                float py = (y - halfH + 0.5f) * cellSize;

                verts.Add(new Vector3(px - cellSize / 2, py - cellSize / 2, 0));
                verts.Add(new Vector3(px + cellSize / 2, py - cellSize / 2, 0));
                verts.Add(new Vector3(px + cellSize / 2, py + cellSize / 2, 0));
                verts.Add(new Vector3(px - cellSize / 2, py + cellSize / 2, 0));

                tris.Add(idx + 0); tris.Add(idx + 2); tris.Add(idx + 1);
                tris.Add(idx + 0); tris.Add(idx + 3); tris.Add(idx + 2);

                uvs.Add(Vector2.zero);
                uvs.Add(Vector2.right);
                uvs.Add(Vector2.one);
                uvs.Add(Vector2.up);

                idx += 4;
            }

        var mesh = new Mesh();
        mesh.indexFormat = verts.Count > 65000 ?
            UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
