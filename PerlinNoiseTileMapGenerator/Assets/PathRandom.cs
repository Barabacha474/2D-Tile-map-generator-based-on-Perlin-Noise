using System.Collections.Generic;
using UnityEngine;

public class PathRandom : MonoBehaviour
{
    [Header("Random Movement Settings")]
    [SerializeField] private Vector2Int startPosition;
    [SerializeField] private Vector2Int goalPosition;
    [SerializeField] private int maxSteps = 1000;
    private int gridWidth;
    private int gridHeight;
    [SerializeField] private List<List<float>> rewards;

    public PathVisualization pathVisualization;

    private List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(0, -1), // Down
        new Vector2Int(1, 0),  // Right
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 1),  // Right-Up
        new Vector2Int(-1, 1), // Left-Up
        new Vector2Int(1, -1),  // Right-Down
        new Vector2Int(-1, -1)  // Left-Down
    };

    public void Init()
    {
        var mapGenerator = FindObjectOfType<PerlinNoiseTileMapGeneration>();
        rewards = mapGenerator.getTileRewards();
        gridHeight = GetComponent<PerlinNoiseTileMapGeneration>().GridHeight;
        gridWidth = GetComponent<PerlinNoiseTileMapGeneration>().GridWidth;

        float totalReward = 0f;

        List<Vector2Int> path = PerformRandomMovement(out totalReward);

        if (pathVisualization != null)
        {
            foreach (var step in path)
            {
                pathVisualization.DrawPixel(step);
            }
        }

        Debug.Log($"Random movement complete. Total Reward: {totalReward}");
    }

    private List<Vector2Int> PerformRandomMovement(out float totalReward)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentPosition = startPosition;
        totalReward = 0f;

        path.Add(currentPosition);
        totalReward += GetReward(currentPosition);

        for (int step = 0; step < maxSteps; step++)
        {
            if (currentPosition == goalPosition)
            {
                Debug.Log($"Goal position reached at step {step}: {currentPosition}");
                break;
            }

            Vector2Int randomDirection = directions[Random.Range(0, directions.Count)];
            Vector2Int nextPosition = currentPosition + randomDirection;

            if (IsPositionValid(nextPosition))
            {
                currentPosition = nextPosition;
                path.Add(currentPosition);
                totalReward += GetReward(currentPosition);
            }
        }

        return path;
    }

    private bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridHeight &&
               position.y >= 0 && position.y < gridWidth;
    }

    private float GetReward(Vector2Int position)
    {
        if (position.x >= 0 && position.x < gridHeight &&
            position.y >= 0 && position.y < gridWidth)
        {
            return rewards[position.x][position.y];
        }
        return 0f;
    }
}
