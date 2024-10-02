using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class PerlinNoiseTileMapGeneration : MonoBehaviour
{

    [SerializeField] private List<GameObject> Tileset;
    [SerializeField] private int GridHeight;
    [SerializeField] private int GridWidth;
    private List<List<GameObject>> Grid;

    [SerializeField] private int Seed = 100;
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
    
    private int[] permutation;
    
    // Start is called before the first frame update
    void Start()
    {
        InitiateGrids();
        GenerateMap();
    }

    void InitiateGrids()
    {
        Random.InitState(Seed); 
        
        Grid = new List<List<GameObject>>();
        for (int i = 0; i < GridHeight; i++)
        {
            Grid.Add(new List<GameObject>());
        }
        
        permutation = new int[GridHeight * GridWidth];
        
        // Permutations used to gradient vector creation of perlin noise
        FillPermutation(Seed);
    }

    void GenerateMap()
    {
        for (int i = 0; i < GridHeight; i++)
        {
            for (int j = 0; j < GridWidth; j++)
            {
                int id = GetTileType(i, j); //get's id between 0 and Tileset.count-1
                GameObject TileType = Tileset[id];
                Grid[i].Add(Instantiate(TileType, new Vector3(i - GridHeight / 2, j - GridWidth / 2, 0), Quaternion.identity));//Generates map in center of coordinates
                Grid[i][j].transform.parent = gameObject.transform;//just for objects organization
            }
        }
    }

    //Gets coordinates, returns id for Tileset
    int GetTileType(float x, float y)
    {
        int output;

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
}
