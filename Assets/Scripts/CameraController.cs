using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Maze Data Source")]
    [Tooltip("Drag your MazeManager here, or leave empty to auto-find one in the scene.")]
    public MazeManager mazeManager;

    [Header("Cell Size (world units)")]
    [Tooltip("The world‐space size of one maze cell. Match this to your MazeMeshBuilder.cellSize (or leave 1).")]
    public float cellSize = 1f;

    [Header("Padding (world units)")]
    [Tooltip("Extra margin around the maze edges.")]
    public float padding = 1f;

    private Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("CameraController requires an attached Camera component.");
            enabled = false;
            return;
        }

        if (mazeManager == null)
        {
            mazeManager = FindAnyObjectByType<MazeManager>();
        }
        if (mazeManager == null)
        {
            Debug.LogError("CameraController: No MazeManager found in the scene.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Read the cell counts from the MazeManager
        int cellsX = mazeManager.cellsX;
        int cellsY = mazeManager.cellsY;

        // Compute the full grid dimensions in cells (including walls)
        int gridWidth = cellsX * 2 + 1;
        int gridHeight = cellsY * 2 + 1;

        // Compute world‐space size
        float worldWidth = gridWidth * cellSize;
        float worldHeight = gridHeight * cellSize;

        // Center the camera on the MazeManager's GameObject
        var mazeTransform = mazeManager.transform;
        transform.position = new Vector3(
            mazeTransform.position.x,
            mazeTransform.position.y,
            transform.position.z
        );

        // Only valid for orthographic cameras
        if (!_camera.orthographic)
        {
            Debug.LogWarning("CameraController works only with an Orthographic camera.");
            return;
        }

        // Determine the required orthographic size
        float halfHeight = worldHeight * 0.5f;
        float halfWidth = (worldWidth * 0.5f) / _camera.aspect;
        float targetSize = Mathf.Max(halfHeight, halfWidth) + padding;

        _camera.orthographicSize = targetSize;
    }
}
