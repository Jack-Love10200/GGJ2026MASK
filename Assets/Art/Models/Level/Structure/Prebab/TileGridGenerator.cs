using UnityEngine;
using System.Collections.Generic;

public class TileGridGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    [Min(1)] public int a = 8;   // columns (X)
    [Min(1)] public int b = 8;   // rows (Z)

    [Header("Tile")]
    public GameObject tilePrefab;

    [Tooltip("Tile world size (assumes tile is 1 unit by 1 unit before scaling).")]
    public float tileSize = 1f;

    [Tooltip("Extra spacing between tiles (world units). 0 = no gap.")]
    public float gap = 0f;

    [Header("Rotation")]
    [Tooltip("Rotation angle around Y-axis for each tile (in degrees).")]
    [Range(0f, 360f)]
    public float rotationAngle = 0f;

    [Header("Options")]
    [Tooltip("If true, grid is centered around this GameObject.")]
    public bool centerGrid = false;

    [Header("Empty Cells (Manual)")]
    [Tooltip("Cells to leave empty. X = column (0~a-1), Y = row (0~b-1).")]
    public Vector2Int[] emptyCells;

    // Public 2D array of spawned tiles (null at empty cells)
    public GameObject[,] tiles;

    void Start()
    {
        Generate();
    }

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("TileGridGenerator: tilePrefab is not assigned.");
            return;
        }

        Clear();

        tiles = new GameObject[a, b];

        float step = tileSize + gap;

        // Optional: center the grid around this transform
        Vector3 origin = transform.position;
        if (centerGrid)
        {
            float totalW = (a - 1) * step;
            float totalH = (b - 1) * step;
            origin = transform.position - new Vector3(totalW * 0.5f, 0f, totalH * 0.5f);
        }

        // Build a fast lookup set (ignore out-of-range values)
        HashSet<Vector2Int> emptySet = new HashSet<Vector2Int>();
        if (emptyCells != null)
        {
            foreach (var cell in emptyCells)
            {
                if (cell.x < 0 || cell.x >= a || cell.y < 0 || cell.y >= b)
                    continue;

                emptySet.Add(cell); // duplicates auto-ignored
            }
        }

        Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);

        for (int col = 0; col < a; col++)
        {
            for (int row = 0; row < b; row++)
            {
                // Skip manual empty cells
                if (emptySet.Contains(new Vector2Int(col, row)))
                {
                    tiles[col, row] = null;
                    continue;
                }

                Vector3 pos = origin + new Vector3(col * step, 0f, row * step);

                GameObject tile = Instantiate(tilePrefab, pos, rotation, transform);
                tile.name = $"Tile_{col}_{row}";

                // Keep prefab Y scale, set footprint to tileSize
                tile.transform.localScale = new Vector3(tileSize, tile.transform.localScale.y, tileSize);

                tiles[col, row] = tile;
            }
        }
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
