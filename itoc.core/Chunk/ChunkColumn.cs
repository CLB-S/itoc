using System.Collections.Concurrent;
using Godot;
using ITOC.Core.Utils;

namespace ITOC.Core;

public class ChunkColumn
{
    // Size of the biome map grid
    public const int BIOME_MAP_SIZE = 5;

    public Vector2I Index;

    /// <summary>
    /// [Chunk.SIZE, Chunk.SIZE], 62x62
    /// </summary>
    public double[,] HeightMap;

    public double HeightMapHigh;
    public double HeightMapLow;
    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks = new();

    private bool _isSurfaceChunksGenerated;
    public bool IsSurfaceChunksGenerated
    {
        get => _isSurfaceChunksGenerated;
        set
        {
            if (_isSurfaceChunksGenerated)
                throw new InvalidOperationException(
                    "Surface chunks have already been generated for this column."
                );

            _isSurfaceChunksGenerated = value;
        }
    }

    // Biome map storage using Palette system
    private readonly PaletteArray<Biome> _biomes;

    private ChunkColumn() { }

    public ChunkColumn(Vector2I index, PaletteArray<Biome> biomes)
    {
        Index = index;

        _biomes = biomes;
    }

    public void SetHeightMap(double[,] heightMap)
    {
        HeightMap = heightMap;
        HeightMapHigh = heightMap.Cast<double>().Max();
        HeightMapLow = heightMap.Cast<double>().Min();
    }

    public static int GetBiomeIndex(int x, int z)
    {
        var index = z + x * BIOME_MAP_SIZE;
        return index;
    }

    private Biome GetBiomeFromPalette(int x, int z) => _biomes[GetBiomeIndex(x, z)];

    private static double CubicInterp(double a, double b, double t)
    {
        var smoothT = t * t * (3.0 - 2.0 * t); // smoothstep function
        return a + smoothT * (b - a);
    }

    /// <summary>
    /// Get the interpolated biome weights at the given normalized position within the chunk
    /// </summary>
    /// <param name="x">world position x</param>
    /// <param name="z">world position z</param>
    /// <returns>Dictionary mapping biomes to their interpolation weights</returns>
    public Dictionary<Biome, double> GetBiomeWeights(double x, double z)
    {
        var normalizedX = Mathf.PosMod(x, Chunk.SIZE) / Chunk.SIZE;
        var normalizedZ = Mathf.PosMod(z, Chunk.SIZE) / Chunk.SIZE;

        var biomeWeights = new Dictionary<Biome, double>();
        normalizedX = (BIOME_MAP_SIZE - 1) * normalizedX;
        normalizedZ = (BIOME_MAP_SIZE - 1) * normalizedZ;
        var x0 = Mathf.FloorToInt(normalizedX);
        var z0 = Mathf.FloorToInt(normalizedZ);
        var x1 = x0 + 1;
        var z1 = z0 + 1;

        // Get the biomes at the four corners
        var topLeftBiome = GetBiomeFromPalette(x0, z0);
        var topRightBiome = GetBiomeFromPalette(x1, z0);
        var bottomLeftBiome = GetBiomeFromPalette(x0, z1);
        var bottomRightBiome = GetBiomeFromPalette(x1, z1);

        // Bicubic interpolation using smoothstep
        // TODO: Still not perfect and have aliasing effect

        var dx = normalizedX - x0;
        var dz = normalizedZ - z0;

        // Calculate weights using bilinear or bicubic interpolation
        double weightTopLeft,
            weightTopRight,
            weightBottomLeft,
            weightBottomRight;

        var interpX = CubicInterp(0, 1, dx);
        var interpZ = CubicInterp(0, 1, dz);

        weightTopLeft = (1.0 - interpX) * (1.0 - interpZ);
        weightTopRight = interpX * (1.0 - interpZ);
        weightBottomLeft = (1.0 - interpX) * interpZ;
        weightBottomRight = interpX * interpZ;

        var totalWeight = weightTopLeft + weightTopRight + weightBottomLeft + weightBottomRight;

        // Add weights to the dictionary
        AddOrUpdateWeight(biomeWeights, topLeftBiome, weightTopLeft / totalWeight);
        AddOrUpdateWeight(biomeWeights, topRightBiome, weightTopRight / totalWeight);
        AddOrUpdateWeight(biomeWeights, bottomLeftBiome, weightBottomLeft / totalWeight);
        AddOrUpdateWeight(biomeWeights, bottomRightBiome, weightBottomRight / totalWeight);

        return biomeWeights;
    }

    private static void AddOrUpdateWeight(
        Dictionary<Biome, double> weights,
        Biome biome,
        double weight
    )
    {
        if (weight <= 0)
            return;

        if (weights.ContainsKey(biome))
            weights[biome] += weight;
        else
            weights[biome] = weight;
    }

    /// <summary>
    ///     Get the dominant biome at the specified position within the chunk
    /// </summary>
    /// <param name="x">world position x</param>
    /// <param name="z">world position z</param>
    /// <returns>The dominant biome at the position</returns>
    public Biome GetDominantBiome(double x, double z)
    {
        var weights = GetBiomeWeights(x, z);
        return weights.OrderByDescending(pair => pair.Value).First().Key;
    }
}
