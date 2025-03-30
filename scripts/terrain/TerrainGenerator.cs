using Godot;
using System;
using System.Collections.Generic;

public class TerrainGenerator
{
    // 配置参数
    public int Seed = 12345;
    public float GlobalScale = 0.6f;

    private FastNoiseLite _baseNoise;
    private FastNoiseLite _detailNoise;
    private FastNoiseLite _voronoiNoise;
    private ErosionProcessor _erosionProcessor;
    private NoiseLayerConfig _config;

    public TerrainGenerator()
    {
        InitializeNoiseGenerators();
        _erosionProcessor = new ErosionProcessor(World.ChunkSize);
        _config = new NoiseLayerConfig();
    }

    private void InitializeNoiseGenerators()
    {
        // 基底噪声配置（低频）
        _baseNoise = new FastNoiseLite();
        _baseNoise.Seed = Seed;
        _baseNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _baseNoise.Frequency = 0.005f;
        _baseNoise.FractalOctaves = 4;
        _baseNoise.FractalLacunarity = 2.0f;
        _baseNoise.FractalGain = 0.5f;

        // 细节噪声配置（高频）
        _detailNoise = new FastNoiseLite();
        _detailNoise.Seed = Seed + 1;
        _detailNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _detailNoise.Frequency = 0.05f;
        _detailNoise.FractalOctaves = 2;

        // Voronoi噪声配置
        _voronoiNoise = new FastNoiseLite();
        _voronoiNoise.Seed = Seed + 2;
        _voronoiNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
        _voronoiNoise.CellularDistanceFunction = FastNoiseLite.CellularDistanceFunctionEnum.Hybrid;
        _voronoiNoise.CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.Distance2Add;
        _voronoiNoise.Frequency = 0.01f;
    }

    /// <summary>
    /// 获取指定区块的高度图
    /// </summary>
    public float[,] GenerateChunkHeightmap(int chunkX, int chunkZ)
    {
        float[,] baseHeight = GenerateBaseHeightmap(chunkX, chunkZ);
        float[,] detailedHeight = AddDetailNoise(baseHeight, chunkX, chunkZ);
        float[,] voronoiHeight = ApplyVoronoiFeatures(detailedHeight, chunkX, chunkZ);
        // float[,] erodedHeight = _erosionProcessor.ApplyErosion(voronoiHeight);

        return voronoiHeight;
    }

    private float[,] GenerateBaseHeightmap(int chunkX, int chunkZ)
    {
        float[,] heightmap = new float[World.ChunkSize, World.ChunkSize];

        int globalX = chunkX * World.ChunkSize;
        int globalZ = chunkZ * World.ChunkSize;

        for (int x = 0; x < World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                float noiseValue = _baseNoise.GetNoise2D(
                    (globalX + x) * GlobalScale,
                    (globalZ + z) * GlobalScale
                );

                // 将噪声值映射到0-1范围
                heightmap[x, z] = (noiseValue + 1) * 0.5f;
            }
        }

        return heightmap;
    }

    private float[,] AddDetailNoise(float[,] baseHeight, int chunkX, int chunkZ)
    {
        float[,] detailed = new float[World.ChunkSize, World.ChunkSize];
        int globalX = chunkX * World.ChunkSize;
        int globalZ = chunkZ * World.ChunkSize;

        for (int x = 0; x < World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                float detail = _detailNoise.GetNoise2D(
                    (globalX + x) * GlobalScale * 2,
                    (globalZ + z) * GlobalScale * 2
                ) * 0.2f;

                detailed[x, z] = Mathf.Clamp(baseHeight[x, z] + detail, 0, 1);
            }
        }

        return detailed;
    }

    private float[,] ApplyVoronoiFeatures(float[,] heightmap, int chunkX, int chunkZ)
    {
        float[,] modified = new float[World.ChunkSize, World.ChunkSize];
        int globalX = chunkX * World.ChunkSize;
        int globalZ = chunkZ * World.ChunkSize;

        for (int x = 0; x < World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                float voronoiValue = _voronoiNoise.GetNoise2D(
                    (globalX + x) * GlobalScale,
                    (globalZ + z) * GlobalScale
                );

                // 文献中的Voronoi系数混合
                float combined = heightmap[x, z] * 0.66f + voronoiValue * 0.34f;
                modified[x, z] = Mathf.Clamp(combined, 0, 1);
            }
        }

        return modified;
    }
}
