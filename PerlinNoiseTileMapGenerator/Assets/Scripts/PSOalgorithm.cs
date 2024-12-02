using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PSOalgorithm : MonoBehaviour
{

    [SerializeField] private PerlinNoiseTileMapGeneration _tilemap;

    private float maxX;
    private float maxY;
    private float minX;
    private float minY;

    [SerializeField] [Range(1, 30)] int numberOfParticlesMaximize;
    [SerializeField] [Range(1, 30)] int numberOfParticlesMinimize;

    private int totalNumberOfParticles;

    [SerializeField] private GameObject particleMaximizePrefab;
    [SerializeField] private GameObject particleMinimizePrefab;

    private float globalMax;
    private Vector2 globalMaxPosition;
    private float globalMin;
    private Vector2 globalMinPostion;

    [SerializeField] private float neighbourhood;

    private float inetrtiaCoef = 0.9f;
    private float globalmax_minCoef = 0.9f;
    private float localmax_minCoef = 0.9f;

    class Particle
    {
        public Vector2 position;
        public Vector2 velocity;
        public float value;
        public float localMax;
        public Vector2 localMaxPosition;
        public float localMin;
        public Vector2 localMinPosition;
        public float globalMax;
        public Vector2 globalMaxPosition;
        public float globalMin;
        public Vector2 globalMinPosition;

        public bool maximize;
    }

    private List<Particle> particles;

    // Start is called before the first frame update
    void Start()
    {
        maxX = _tilemap.GridWidth / 2.0f;
        minX = -maxX;
        maxY = _tilemap.GridHeight / 2.0f;
        minY = -minY;

        totalNumberOfParticles = numberOfParticlesMinimize + numberOfParticlesMaximize;

        particles = new List<Particle>();
        for (int i = 0; i < totalNumberOfParticles; i++)
        {
            Particle particle = new Particle();

            Vector2 position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            float value = _tilemap.GetPerlinNoiseValue(position.x, position.y);
            particle.position = position;
            particle.localMinPosition = position;
            particle.localMaxPosition = position;
            particle.value = value;
            particle.localMax = value;
            particle.localMin = value;

            if (i < numberOfParticlesMaximize)
            {
                particle.maximize = true;
            }
            else
            {
                particle.maximize = false;
            }

            particles.Add(particle);
        }

        for (int i = 0; i < totalNumberOfParticles; i++)
        {
            setGlobalMaxMin(i);
            Debug.Log(particles[i]);
        }
    }

    private void setGlobalMaxMin(int index)
    {
        if (index < 0 || index >= totalNumberOfParticles)
        {
            throw new Exception("Index out of particles list bounds!");
        }

        for (int i = 0; i < totalNumberOfParticles; i++)
        {
            if (i != index)
            {
                if (Vector2.Distance(particles[i].position, particles[index].position) <= neighbourhood)
                {
                    if (particles[index].globalMax < particles[i].value)
                    {
                        particles[index].globalMax = particles[i].value;
                        particles[index].globalMaxPosition = particles[i].position;
                    }

                    if (particles[index].globalMin > particles[i].value)
                    {
                        particles[index].globalMin = particles[i].value;
                        particles[index].globalMinPosition = particles[i].position;
                    }
                }
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
