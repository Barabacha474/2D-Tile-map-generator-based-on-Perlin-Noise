using System.Collections.Generic;
using UnityEngine;

public class PathVisualization : MonoBehaviour
{
    public Vector2Int PixelPosition; // The position of the red pixel (input)

    public int GridWidth = 150; // Width of the grid
    public int GridHeight = 150; // Height of the grid
    public GameObject PixelPrefab; // Prefab for a pixel (a small square or cube)

    void Start()
    {
        // Draw the pixel at the specified position
        DrawPixel(PixelPosition);
    }

    public void DrawPixel(Vector2Int position)
    {
        // Ensure the pixel position is within grid bounds
        if (position.x >= 0 && position.x < GridWidth && position.y >= 0 && position.y < GridHeight)
        {
            // Instantiate a red pixel at the specified position
            GameObject pixel = Instantiate(PixelPrefab, new Vector3(position.x - GridWidth / 2, position.y - GridHeight / 2, 1), Quaternion.identity);
            
            // Set the pixel color to red
            var renderer = pixel.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.red;
            }
        }
        else
        {
            Debug.LogWarning("Pixel position is out of grid bounds.");
        }
    }
}