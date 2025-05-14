using UnityEngine;

public class MazeManager : MonoBehaviour
{
 
    public static MazeManager Instance { get; private set; }

    [Header("Maze Dimensions (cells)")]
    public int cellsX = 10;
    public int cellsY = 10;

    public float cellSize = 1f;

    [HideInInspector]
    public int[,] maze;

    void Awake()
    {
        // Enforce singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // (Optional) persist across scenes:
        // DontDestroyOnLoad(gameObject);

        // Generate the maze exactly once
        maze = MazeGenerator.Generate(cellsX, cellsY);
    }
}
