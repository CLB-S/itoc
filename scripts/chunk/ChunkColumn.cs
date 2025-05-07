using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Palette;

public class ChunkColumn
{
    // Size of the biome map grid
    public const int BIOME_MAP_SIZE = 5;

    public Vector2I Position;

    /// <summary>
    /// [ChunkMesher.CS, ChunkMesher.CS], 62x62
    /// </summary>
    public double[,] HeightMap;
    public double HeightMapHigh;
    public double HeightMapLow;
    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks = new();

    // Biome map storage using Palette system
    private readonly PaletteStorage<Biome> _biomePaletteStorage;

    private ChunkColumn()
    {
    }

    public ChunkColumn(Vector2I position, Palette<Biome> biomePalette, PaletteStorage<Biome> biomePaletteStorage)
    {
        Position = position;

        _biomePaletteStorage = biomePaletteStorage;
    }

    public void SetHeightMap(double[,] heightMap)
    {
        HeightMap = heightMap;
        HeightMapHigh = heightMap.Cast<double>().Max();
        HeightMapLow = heightMap.Cast<double>().Min();
    }

    public static int GetBiomeIndex(int x, int z)
    {
        int index = z + x * BIOME_MAP_SIZE;
        return index;
    }

    private Biome GetBiomeFromPalette(int x, int z)
    {
        return _biomePaletteStorage.Get(GetBiomeIndex(x, z));
    }

    /// <summary>
    /// Get the interpolated biome weights at the given normalized position within the chunk
    /// </summary>
    /// <returns>Dictionary mapping biomes to their interpolation weights</returns>
    public Dictionary<Biome, double> GetBiomeWeights(double normalizedX, double normalizedZ)
    {
        var biomeWeights = new Dictionary<Biome, double>();
        normalizedX = (BIOME_MAP_SIZE - 1) * normalizedX;
        normalizedZ = (BIOME_MAP_SIZE - 1) * normalizedZ;
        int x0 = Mathf.FloorToInt(normalizedX);
        int z0 = Mathf.FloorToInt(normalizedZ);
        int x1 = x0 + 1;
        int z1 = z0 + 1;

        // Get the biomes at the four corners
        var topLeftBiome = GetBiomeFromPalette(x0, z0);
        var topRightBiome = GetBiomeFromPalette(x1, z0);
        var bottomLeftBiome = GetBiomeFromPalette(x0, z1);
        var bottomRightBiome = GetBiomeFromPalette(x1, z1);

        // Calculate weights for each corner
        var weightTopLeft = Mathf.Max(1 - Mathf.Sqrt(Mathf.Pow(normalizedX - x0, 2) + Mathf.Pow(normalizedZ - z0, 2)), 0);
        var weightTopRight = Mathf.Max(1 - Mathf.Sqrt(Mathf.Pow(normalizedX - x1, 2) + Mathf.Pow(normalizedZ - z0, 2)), 0);
        var weightBottomLeft = Mathf.Max(1 - Mathf.Sqrt(Mathf.Pow(normalizedX - x0, 2) + Mathf.Pow(normalizedZ - z1, 2)), 0);
        var weightBottomRight = Mathf.Max(1 - Mathf.Sqrt(Mathf.Pow(normalizedX - x1, 2) + Mathf.Pow(normalizedZ - z1, 2)), 0);
        var totalWeight = weightTopLeft + weightTopRight + weightBottomLeft + weightBottomRight;

        // Add weights to the dictionary
        AddOrUpdateWeight(biomeWeights, topLeftBiome, weightTopLeft / totalWeight);
        AddOrUpdateWeight(biomeWeights, topRightBiome, weightTopRight / totalWeight);
        AddOrUpdateWeight(biomeWeights, bottomLeftBiome, weightBottomLeft / totalWeight);
        AddOrUpdateWeight(biomeWeights, bottomRightBiome, weightBottomRight / totalWeight);

        return biomeWeights;
    }

    private static void AddOrUpdateWeight(Dictionary<Biome, double> weights, Biome biome, double weight)
    {
        if (weight <= 0) return;

        if (weights.ContainsKey(biome))
            weights[biome] += weight;
        else
            weights[biome] = weight;
    }

    /// <summary>
    /// Get the dominant biome at the specified position within the chunk
    /// </summary>
    /// <returns>The dominant biome at the position</returns>
    public Biome GetDominantBiome(int x, int y)
    {
        var weights = GetBiomeWeights(x, y);
        return weights.OrderByDescending(pair => pair.Value).First().Key;
    }
}