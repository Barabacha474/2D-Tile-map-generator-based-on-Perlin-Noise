using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    [SerializeField] private Vector2Int startPosition;
    [SerializeField] private Vector2Int goalPosition;
    [SerializeField] private float learningRate = 0.1f;
    [SerializeField] private float discountFactor = 0.9f;
    [SerializeField] private int maxIterations = 1000;
    [SerializeField] private float explorationRate = 0.2f;
    [SerializeField] private int maxStepsPerEpisode = 500;
    [SerializeField] private int maxRetries = 3;

    private List<List<float>> rewards;
    private List<List<float>> qValues;
    private int gridWidth;
    private int gridHeight;
    private List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(0, -1), // Down
        new Vector2Int(1, 0),  // Right
        new Vector2Int(-1, 0)  // Left
    };

    public void Init()
    {
        var mapGenerator = FindObjectOfType<PerlinNoiseTileMapGeneration>();
        rewards = mapGenerator.getTileRewards();
        gridHeight = rewards.Count;
        gridWidth = rewards[0].Count;

        InitializeQValues();

        TrainAgent();

        List<Vector2Int> bestPath = FindBestPath();
        foreach (var step in bestPath)
        {
            Debug.Log($"Step: {step}");
        }
    }

    private void InitializeQValues()
    {
        qValues = new List<List<float>>();
        for (int i = 0; i < gridHeight; i++)
        {
            qValues.Add(new List<float>());
            for (int j = 0; j < gridWidth; j++)
            {
                qValues[i].Add(0f);
            }
        }
    }

    private void TrainAgent()
    {
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            Vector2Int currentPosition = startPosition;
            HashSet<Vector2Int> visitedPositions = new HashSet<Vector2Int>();
            int steps = 0;

            while (currentPosition != goalPosition && steps < maxStepsPerEpisode)
            {
                steps++;
                visitedPositions.Add(currentPosition);

                Vector2Int action;
                if (Random.value < explorationRate)
                {
                    action = directions[Random.Range(0, directions.Count)];
                }
                else
                {
                    action = GetBestAction(currentPosition);
                }

                Vector2Int nextPosition = currentPosition + action;

                if (IsPositionValid(nextPosition))
                {
                    float reward = rewards[nextPosition.x][nextPosition.y];
                    float maxFutureQ = GetMaxQValue(nextPosition);
                    qValues[currentPosition.x][currentPosition.y] += learningRate *
                        (reward + discountFactor * maxFutureQ - qValues[currentPosition.x][currentPosition.y]);

                    currentPosition = nextPosition;
                }
            }

            if (steps >= maxStepsPerEpisode)
            {
                Debug.LogWarning($"Reached max steps for iteration {iteration}, stopping episode.");
            }
        }
    }



    private List<Vector2Int> FindBestPath()
    {
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            List<Vector2Int> path = AttemptPathFinding();

            if (path.Count > 0)
            {
                Debug.Log($"Pathfinding succeeded after {retryCount + 1} attempt(s).");
                return path;
            }

            retryCount++;
            Debug.LogWarning($"Pathfinding attempt {retryCount} failed. Retrying...");
        }

        Debug.LogError("Pathfinding failed after maximum retries.");
        return new List<Vector2Int>();
    }

    private List<Vector2Int> AttemptPathFinding()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentPosition = startPosition;
        HashSet<Vector2Int> visitedPositions = new HashSet<Vector2Int>();
        int maxPathLength = gridWidth * gridHeight;

        while (currentPosition != goalPosition)
        {
            Debug.Log($"Current Position: {currentPosition}, Path Length: {path.Count}");

            if (path.Count > maxPathLength)
            {
                Debug.LogError("Pathfinding failed: Exceeded maximum path length.");
                return new List<Vector2Int>();
            }

            if (visitedPositions.Contains(currentPosition))
            {
                // Debug.LogError("Pathfinding failed: Encountered a cycle.");
                // return new List<Vector2Int>();
            }

            visitedPositions.Add(currentPosition);
            path.Add(currentPosition);

            currentPosition += GetBestAction(currentPosition);

            if (!IsPositionValid(currentPosition))
            {
                Debug.LogError("Pathfinding failed: Reached an invalid position.");
                return new List<Vector2Int>();
            }
        }

        path.Add(goalPosition);
        return path;
    }



    private Vector2Int GetBestAction(Vector2Int position)
    {
        Vector2Int bestAction = directions[0];
        float bestQValue = float.MinValue;

        foreach (var direction in directions)
        {
            Vector2Int nextPosition = position + direction;
            if (IsPositionValid(nextPosition))
            {
                float qValue = qValues[nextPosition.x][nextPosition.y];
                if (qValue > bestQValue)
                {
                    bestQValue = qValue;
                    bestAction = direction;
                }
            }
        }

        return bestAction;
    }

    private float GetMaxQValue(Vector2Int position)
    {
        float maxQValue = float.MinValue;

        foreach (var direction in directions)
        {
            Vector2Int nextPosition = position + direction;
            if (IsPositionValid(nextPosition))
            {
                maxQValue = Mathf.Max(maxQValue, qValues[nextPosition.x][nextPosition.y]);
            }
        }

        return maxQValue;
    }

    private bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridHeight &&
               position.y >= 0 && position.y < gridWidth;
    }
}
