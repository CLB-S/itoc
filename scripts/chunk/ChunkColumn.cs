using Godot;
using System.Collections.Concurrent;
using System.Linq;

public class ChunkColumn
{
    public Vector2I Position;
    public float[,] HeightMap;
    public float HeightMapHigh;
    public float HeightMapLow;
    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks = new();


    private ChunkColumn() { }
    public ChunkColumn(Vector2I position, float[,] heightMap)
    {
        Position = position;
        HeightMap = heightMap;

        HeightMapHigh = heightMap.Cast<float>().Max();
        HeightMapLow = heightMap.Cast<float>().Min();
    }
}