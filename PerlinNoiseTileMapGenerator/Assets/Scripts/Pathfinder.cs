using System.Collections.Generic;
using System.IO;
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
    [SerializeField] private int maxStepsForPathfinding = 1000;

    private List<Vector2Int> lastBadPath;
    public PathVisualization pathVisualization;

    private List<List<float>> rewards;
    private List<List<float>> qValues;
    private int gridWidth;
    private int gridHeight;
    private bool isReached = false;
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
        gridHeight = rewards.Count;
        gridWidth = rewards[0].Count;

        rewards[goalPosition.x][goalPosition.y] += 10f;

        InitializeQValues();

        TrainAgent();

        List<Vector2Int> bestPath = FindBestPath();
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
                float reward = 0;
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

                    // Add noise if oscillation is likely
                    if (visitedPositions.Contains(currentPosition + action))
                    {
                        reward -= 0.5f; // Stronger penalty for revisits
                        action = directions[Random.Range(0, directions.Count)];
                    }
                }


                Vector2Int nextPosition = currentPosition + action;

                if (IsPositionValid(nextPosition))
                {
                    float distancePenalty = Vector2Int.Distance(nextPosition, goalPosition) * 0.1f; // Add penalty based on distance
                    reward += rewards[nextPosition.x][nextPosition.y] - distancePenalty;

                    if (visitedPositions.Contains(nextPosition))
                    {
                        reward -= 0.1f; // Apply a penalty for revisiting
                    }

                    float maxFutureQ = GetMaxQValue(nextPosition);
                    qValues[currentPosition.x][currentPosition.y] += learningRate *
                        (reward + discountFactor * maxFutureQ - qValues[currentPosition.x][currentPosition.y]);


                    currentPosition = nextPosition;
                }
            }

            // explorationRate = Mathf.Max(0.1f, explorationRate * 0.99f);

            if (steps >= maxStepsPerEpisode)
            {
                // Debug.LogWarning($"Reached max steps for iteration {iteration}.");
            }
        }
    }




    private List<Vector2Int> FindBestPath()
    {
        int retryCount = 0;
        float totalReward = 0;

        while (retryCount < maxRetries)
        {
            List<Vector2Int> path = AttemptPathFinding();

            if (isReached)
            {
                foreach (var step in path)
                {
                    Debug.Log($"Step: {step}");
                    pathVisualization.DrawPixel(step);

                    totalReward += rewards[step.x][step.y];
                }

                Debug.Log($"Pathfinding succeeded after {retryCount + 1} attempt(s).");
                Debug.Log($"Total Reward of the Path: {totalReward}");
                return path;
            }

            retryCount++;

            foreach (var step in path)
            {
                Debug.Log($"Step: {step}");
            }

            Debug.LogWarning($"Pathfinding attempt {retryCount} failed. Retrying...");
            lastBadPath = path;
        }

        foreach (var step in lastBadPath)
        {
            Debug.Log($"Step: {step}");
            pathVisualization.DrawPixel(step);

            totalReward += rewards[step.x][step.y];
        }
        Debug.LogError("Pathfinding failed after maximum retries.");
        Debug.Log($"Total Reward of the Last Attempt: {totalReward}");
        return new List<Vector2Int>();
    }

    private List<Vector2Int> AttemptPathFinding()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentPosition = startPosition;
        Queue<Vector2Int> recentPositions = new Queue<Vector2Int>();
        int cycleLength = 4;
        int steps = 0;

        while (currentPosition != goalPosition)
        {
            // Debug.Log($"Current Position: {currentPosition}, Path Length: {path.Count}");

            if (steps >= maxStepsForPathfinding)
            {
                Debug.LogWarning("Step limit reached during pathfinding.");
                return path;
            }

            steps++;

            if (recentPositions.Count == cycleLength)
            {
                recentPositions.Dequeue();
            }
            recentPositions.Enqueue(currentPosition);

            path.Add(currentPosition);

            Vector2Int nextAction = GetBestAction(currentPosition);

            // Add noise if oscillation is likely
            if (recentPositions.Contains(currentPosition + nextAction))
            {
                // Debug.Log("Applying random noise to break potential oscillation.");
                nextAction = directions[Random.Range(0, directions.Count)];
            }

            currentPosition += nextAction;


            if (!IsPositionValid(currentPosition))
            {
                Debug.LogError("Reached an invalid position.");
                return new List<Vector2Int>();
            }
        }

        path.Add(goalPosition);
        isReached = true;
        return path;
    }




    private Vector2Int GetBestAction(Vector2Int position)
    {
        Vector2Int bestAction = Vector2Int.zero;
        float bestQValue = float.MinValue;

        foreach (var direction in directions)
        {
            Vector2Int nextPosition = position + direction;
            if (IsPositionValid(nextPosition))
            {
                float qValue = qValues[nextPosition.x][nextPosition.y];
                float heuristic = Vector2Int.Distance(nextPosition, goalPosition);
                qValue -= heuristic * 0.1f;

                if (qValue > bestQValue)
                {
                    bestQValue = qValue;
                    bestAction = direction;
                }
            }
        }

        if (bestAction == Vector2Int.zero)
        {
            foreach (var direction in directions)
            {
                Vector2Int fallbackPosition = position + direction;
                if (IsPositionValid(fallbackPosition))
                {
                    return direction;
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
