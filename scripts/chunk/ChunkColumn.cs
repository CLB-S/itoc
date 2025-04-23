using Godot;
using System.Collections.Concurrent;
using System.Linq;

public class ChunkColumn
{
    public Vector2I Position;
    public double[,] HeightMap;
    public double HeightMapHigh;
    public double HeightMapLow;
    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks = new();

    private ChunkColumn() { }
    public ChunkColumn(Vector2I position, double[,] heightMap)
    {
        Position = position;
        HeightMap = heightMap;

        HeightMapHigh = heightMap.Cast<double>().Max();
        HeightMapLow = heightMap.Cast<double>().Min();
    }
}