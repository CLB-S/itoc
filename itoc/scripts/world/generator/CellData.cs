using DelaunatorSharp;
using Godot;

namespace ITOC.WorldGeneration;

public enum PlateType
{
    Continent,
    Oceans
}


public class CellData
{
    public int Index => Cell.Index;
    public Vector2 Position;
    public VoronoiCell Cell;
    public Vector2 TectonicMovement;
    public PlateType PlateType;
    public uint PlateSeed;
    public double Uplift = 0.1;
    public double Height = 0;
    public Vector3 Normal = Vector3.Up;
    public double Area = 0;
    public bool IsRiverMouth = false;
    public bool RoundPlateJunction = false;
    public double Precipitation = 0;
    public double Temperature = 0;
    public Biome Biome;

    public override string ToString()
    {
        return
            $"Cell {Index}: Type={PlateType}, Uplift={Uplift:f2}, Height={Height:f2}, Normal=({Normal.X:f2}, {Normal.Y:f2}, {Normal.Z:f2}), " +
            $"Area={Area:f2}, Precipitation={Precipitation:f2}, Temperature={Temperature:f2}, Biome={Biome?.Id ?? "None"}";
    }
}