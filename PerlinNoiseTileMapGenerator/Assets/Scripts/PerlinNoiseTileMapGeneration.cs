using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.Serialization;

public class PerlinNoiseTileMapGeneration : MonoBehaviour
{
    [SerializeField] private bool isRandom;

    [Header("Sets")]
    [SerializeField] private List<Tile> Tileset;
    [System.Serializable]
    public class Tile
    {
        public GameObject tile;
        public float reward;
    }

    [SerializeField] private List<Obstacle> ObstaclesSet;
    [System.Serializable]
    public class Obstacle
    {
        public GameObject obstacle;
        public float reward_modification;
        public List<int> SpawnOnTileTypeIndexes;
    }
    [Header("Map")]
    [SerializeField] public int GridHeight; 
    [SerializeField] public int GridWidth;
    private List<List<GameObject>> Grid;
    private List<List<int>> TileTypes;
    private List<List<float>> TileRewards;

    [SerializeField] private int MapSeed = 100;
    
    [SerializeField]
    private float magnification = 7f;
    [SerializeField]
    [Range(0, 1000)]
    private int x_offset = 0;
    [SerializeField]
    [Range(0, 1000)]
    private int y_offset = 0;
    
    [Range(0, 1)] [SerializeField] private float MathFUsagePercantage = 0.95f;
    [Range(1, 100)]
    [SerializeField]private float Persistance = 1;
    [Range(1, 100)]
    [SerializeField] private int Octaves = 1;

    [SerializeField] private bool Visualize = true;
    [SerializeField] private bool Generate_obstacles = true;
    [SerializeField] private bool PrintOutputs = true;

    [Header("Obstacles")]
    [SerializeField] private int ObstaclesSeed = 100;
    [SerializeField] private int NumberOfRoads = 5;
    [SerializeField] private float NewRoadPointMaxRange = 20;
    [SerializeField] private float NewRoadPointMinRange = 10;
    [SerializeField] private int NumberOfBuildings = 10;

    private int[] permutation;
    
    // Start is called before the first frame update
    void Start()
    {
        
        InitiateGrids();
        GenerateMap();
        if (Generate_obstacles)
        {
            GenerateObstacles();
        }
        if (Visualize)
        {
            VisualizeGrid();
        }
        if (PrintOutputs)
        {
            PrintOutput();
        }

        if (!isRandom)
        {
            GetComponent<Pathfinder>().Init();
        }
        else
        {
            GetComponent<PathRandom>().Init();
        }
    }

    void InitiateGrids()
    {
        Random.InitState(MapSeed);

        Grid = new List<List<GameObject>>();
        TileTypes = new List<List<int>>();
        TileRewards = new List<List<float>>();
        for (int i = 0; i < GridHeight; i++)
        {
            Grid.Add(new List<GameObject>());
            TileTypes.Add(new List<int>());
            TileRewards.Add(new List<float>());
            for (int j = 0; j < GridWidth; j++)
            {
                TileTypes[i].Add(0);
                TileRewards[i].Add(0);
            }
        }
        
        permutation = new int[GridHeight * GridWidth];
        
        // Permutations used to gradient vector creation of perlin noise
        FillPermutation(MapSeed);
        
    }

    void GenerateMap()
    {
        for (int i = 0; i < GridHeight; i++)
        {
            for (int j = 0; j < GridWidth; j++)
            {
                int id = GetTileType(i, j); //get's id between 0 and Tileset.count-1


                TileTypes[i][j] = id;
                TileRewards[i][j] = Tileset[id].reward;
            }
        }
    }

    void GenerateObstacles()
    {
        Random.InitState(ObstaclesSeed);
        List<Vector2Int> roadPoints = GenerateRoadAndBridgePoints();
        
        GenerateBuildings(roadPoints);
    }

    List<Vector2Int> GenerateRoadAndBridgePoints()
    {
        List<Vector2Int> roadPoints = new List<Vector2Int>();
        Vector2Int startPoint = new Vector2Int(GridHeight + 1, GridWidth + 1);
        Vector2Int endPoint = new Vector2Int(GridHeight + 1, GridWidth + 1);
        for (int r = 0; r < NumberOfRoads; r++)
        {
            while (!IsValidRoadTile(startPoint))
            {
                startPoint = new Vector2Int(Random.Range(0, GridHeight), Random.Range(0, GridWidth));
            }


            while ((!IsValidRoadTile(endPoint) || startPoint == endPoint)
                || (Vector2.Distance(startPoint, endPoint) < NewRoadPointMinRange || Vector2.Distance(startPoint, endPoint) > NewRoadPointMaxRange))
            {
                endPoint = new Vector2Int(Random.Range(0, GridHeight), Random.Range(0, GridWidth));
            }

            List<Vector2Int> path = ConnectPointsWithRoadAndBridges(startPoint, endPoint);
            roadPoints.AddRange(path);

            startPoint = endPoint;
        }
        return roadPoints;
    }

    void GenerateBuildings(List<Vector2Int> roadPoints)
    {
        for (int b = 0; b < NumberOfBuildings; b++)
        {
            Vector2Int nearRoad;
            do
            {
                nearRoad = roadPoints[Random.Range(0, roadPoints.Count)];
                nearRoad += new Vector2Int(Random.Range(1, 5), Random.Range(1, 5));
            } while (!IsValidBuildingTile(nearRoad));

            int size = Random.Range(1, 3);
            PlaceBuilding(nearRoad, size);
        }
    }

    List<Vector2Int> ConnectPointsWithRoadAndBridges(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2 current = start;
        var cur_position = Vector2Int.RoundToInt(current);
        while (Vector2Int.RoundToInt(current) != end)
        {
            cur_position = Vector2Int.RoundToInt(current);
            path.Add(cur_position);
            if (IsValidRoadTile(cur_position))
            {
                PlaceObstacle(cur_position, 0); // Assuming 0 is the road obstacle index
            } else if (IsValidBridgeTile(cur_position)) {
                PlaceObstacle(cur_position, 1); // Assuming 1 is the bridge obstacle index
            }
            
            current = Vector2.MoveTowards(current, end, 1);
        }
        path.Add(end);
        PlaceObstacle(end, 0);
        return path;
    }

    void PlaceBuilding(Vector2Int center, int size)
    {
        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);
                if (IsValidBuildingTile(pos))
                {
                    PlaceObstacle(pos, 2); // Assuming 2 is the building obstacle index
                }
            }
        }
    }

    void PlaceObstacle(Vector2Int position, int obstacleIndex)
    {
        if (IsInBounds(position))
        {
            TileTypes[position.x][position.y] = Tileset.Count + obstacleIndex;
            TileRewards[position.x][position.y] += ObstaclesSet[obstacleIndex].reward_modification;
        }
    }

    bool IsValidRoadTile(Vector2Int position)
    {
        return IsInBounds(position) && (TileTypes[position.x][position.y] >= ObstaclesSet[0].SpawnOnTileTypeIndexes[0] // Assuming 0 is the road obstacle index
                                    && TileTypes[position.x][position.y] <= ObstaclesSet[0].SpawnOnTileTypeIndexes[ObstaclesSet[0].SpawnOnTileTypeIndexes.Count - 1]
                                    || TileTypes[position.x][position.y] == Tileset.Count + 0);
    }

    bool IsValidBridgeTile(Vector2Int position)
    {
        return IsInBounds(position) && (TileTypes[position.x][position.y] >= ObstaclesSet[1].SpawnOnTileTypeIndexes[0] // Assuming 1 is the bridge obstacle index
                                    && TileTypes[position.x][position.y] <= ObstaclesSet[1].SpawnOnTileTypeIndexes[ObstaclesSet[1].SpawnOnTileTypeIndexes.Count - 1]
                                    || TileTypes[position.x][position.y] == Tileset.Count + 1);
    }

    bool IsValidBuildingTile(Vector2Int position)
    {
        return IsInBounds(position) && (TileTypes[position.x][position.y] >= ObstaclesSet[2].SpawnOnTileTypeIndexes[0] // Assuming 2 is the building obstacle index
                                    && TileTypes[position.x][position.y] <= ObstaclesSet[2].SpawnOnTileTypeIndexes[ObstaclesSet[2].SpawnOnTileTypeIndexes.Count - 1]
                                    || TileTypes[position.x][position.y] == Tileset.Count + 2)
                                    && !IsRoadOrBridge(position);
    }

    bool IsRoadOrBridge(Vector2Int position)
    {
        return TileTypes[position.x][position.y] >= Tileset.Count && TileTypes[position.x][position.y] <= Tileset.Count + 1;
    }

    bool IsInBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < GridHeight && position.y >= 0 && position.y < GridWidth;
    }

    void PrintOutput()
    {
        var StringTileTypes = "";
        var StringTileRewards = "";

        for (int i = 0; i < GridHeight; i++)
        {
            for (int j = 0; j < GridWidth; j++)
            {
                StringTileTypes += TileTypes[i][j] + " ";

                StringTileRewards += TileRewards[i][j] + " ";
            }
            StringTileTypes += "\n";

            StringTileRewards += "\n";
        }

        Debug.Log(StringTileTypes);
        Debug.Log(StringTileRewards);
    }

    void VisualizeGrid()
    {
        for (int i = 0; i < GridHeight; i++)
        {
            for (int j = 0; j < GridWidth; j++)
            {
                var TileTypeID = TileTypes[i][j];
                GameObject TileType;
                if (TileTypeID < Tileset.Count)
                {
                    TileType = Tileset[TileTypeID].tile;
                }
                else
                {
                    TileType = ObstaclesSet[TileTypeID - Tileset.Count].obstacle;
                }        
                Grid[i].Add(Instantiate(TileType, new Vector3(i - GridHeight / 2, j - GridWidth / 2, 0), Quaternion.identity));//Generates map in center of coordinates
                Grid[i][j].transform.parent = gameObject.transform;//just for objects organization
            }
        }
    }

    public float GetPerlinNoiseValue(float x, float y)
    {
        if (magnification == 0f) //to prevent 0 division
        {
            magnification = 1f;
        }
        
        //noise image shift
        x += x_offset;
        y += y_offset;

        //noise image magnification
        x /= magnification;
        y /= magnification;

        //on this value will be based output
        float perlinValue = 0;

        if (MathFUsagePercantage != 0)
        {
            perlinValue += Mathf.PerlinNoise(x, y) * MathFUsagePercantage;
        }

        if (MathFUsagePercantage != 1)
        {
            perlinValue += PerlinNoise(x, y, Persistance, Octaves) * (1f - MathFUsagePercantage);
        }

        //Sometimes perlin noise produces values below 0 or above 1, which is inappropriate for this case
        perlinValue = Mathf.Clamp(perlinValue, 0f, 1f);

        //Scale it to the number of Tile type in Typeset
        perlinValue *= Tileset.Count;

        return perlinValue;

    }
    
    //Gets coordinates, returns id for Tileset
    int GetTileType(float x, float y)
    {
        int output;

        float perlinValue = GetPerlinNoiseValue(x, y);
        
        //Turn to id
        output = Mathf.FloorToInt(perlinValue);

        //output Clamp
        if (output < 0)
        {
            output = 0;
        }

        if (output > Tileset.Count - 1)
        {
            output = Tileset.Count - 1;
        }

        return output;
    }
    
    //Generates array from 0 to GridHeight * GridWidth and shuffles it to create pseudo random  
    void FillPermutation(int seed)
    {
        var random = new System.Random(seed);
        for (int i = 0; i < GridHeight * GridWidth; i++)
        {
            permutation[i] = i;
        }

        for (int i = 0; i < GridHeight * GridWidth; i++)
        {
            int j = random.Next(GridHeight * GridWidth);
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }
    }

    //Fade function for the Perlin noise. Can be replaced, for example on Cosine: (1 - Cos(t * PI))/2 or Cubic: -2t^3+3t^2
    float QunticCurve(float t)
    {
        return Mathf.Pow(t, 3) * ( (t * 6 - 15) * t + 10);
    }


    //Linear interpolation function for the Perlin noise
    float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    //Determines gradient of x and y based on pseudo random value vector
    float Grad(int vector, float x, float y)
    {
        vector %= 4;
        switch (vector)
        {
            case 0:  return x + y;
            case 1:  return x - y;
            case 2:  return - x + y;
            default: return - x - y;
        }
    }
    
    //Produce Perlin noise
    float Noise(float x, float y)
    {
        //Used for pseudo random gradient vector generation
        int xi = (int)x & (GridHeight * GridWidth - 1);
        int yi = (int)y & (GridHeight * GridWidth - 1);
        
        //local position of x and y between it's edge integers
        float local_x = x - Mathf.FloorToInt(x);
        float local_y = y - Mathf.FloorToInt(y);

        //Fade of local coordinates
        float u = QunticCurve(local_x);
        float v = QunticCurve(local_y);

        //Gradient vectors values
        int topLeftGradientVector = permutation[permutation[xi] + yi];
        int topRightGradientVector = permutation[permutation[xi] + yi + 1];
        int bottomLeftGradientVector = permutation[permutation[xi + 1] + yi];
        int bottomRightGradientVector = permutation[permutation[xi + 1] + yi + 1];

        //Values of applied gradient
        float tx1 = Grad(topLeftGradientVector, local_x, local_y);
        float tx2 = Grad(bottomLeftGradientVector, local_x - 1, local_y);
        float bx1 = Grad(topRightGradientVector, local_x, local_y - 1);
        float bx2 = Grad(bottomRightGradientVector, local_x - 1, local_y - 1);
        
        //Linear interpolation
        float x1 = Lerp(u, tx1, tx2);
        float x2 = Lerp(u, bx1, bx2);

        return Lerp(v, x1, x2);
    }
    
    /// <summary>
    /// Call Noise function with given hyperparametres
    /// </summary>
    /// <param name="x">X coordinate of point</param>
    /// <param name="y">Y coordinate of point</param>
    /// <param name="persistence">Speed of noise amplitude increase</param>
    /// <param name="octaves">Number of noise iteration</param>
    /// <returns>value between 0 and 1 (rarely can produce beyond this range)</returns>
    float PerlinNoise(float x, float y, float persistence, int octaves)
    {
        float total = 0; //sum of noises
        float frequency = 1; //similar to magnification
        float amplitude = 1; //multiplier of noise values
        float maxValue = 0; //denominator, used to range output in range from 0 to 1

        for (int i = 0; i < octaves; i++)
        {
            total += Noise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }

    public List<List<int>> getTileTypes()
    {
        return TileTypes;
    }

    public List<List<float>> getTileRewards()
    {
        return TileRewards;
    }
}
