using Godot;
using System.Linq;

public class ChunkColumn
{
    public Vector2I Position;
    public float[,] HeightMap;
    public float HeightMapHigh;
    public float HeightMapLow;

    private ChunkColumn() { }
    public ChunkColumn(Vector2I position, float[,] heightMap)
    {
        Position = position;
        HeightMap = heightMap;

        HeightMapHigh = heightMap.Cast<float>().Max();
        HeightMapLow = heightMap.Cast<float>().Min();
    }
}