using System.Collections.Generic;
using UnityEngine;

public class PathVisualization : MonoBehaviour
{
    public Vector2Int PixelPosition;

    private int GridWidth;
    private int GridHeight;
    public Color color = Color.red;
    public GameObject PixelPrefab;

    void Start()
    {
        GridWidth = FindAnyObjectByType<PerlinNoiseTileMapGeneration>().GridWidth;
        GridHeight = FindAnyObjectByType<PerlinNoiseTileMapGeneration>().GridHeight;

        DrawPixel(PixelPosition);
    }

    public void DrawPixel(Vector2Int position)
    {
        if (position.x >= 0 && position.x < GridWidth && position.y >= 0 && position.y < GridHeight)
        {
            GameObject pixel = Instantiate(PixelPrefab, new Vector3(position.x - GridWidth / 2, position.y - GridHeight / 2, -1), Quaternion.identity, transform);
            
            var renderer = pixel.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
        else
        {
            Debug.LogWarning("Pixel position is out of grid bounds.");
        }
    }
}