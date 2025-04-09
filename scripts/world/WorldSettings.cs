using Godot;
using System;

public class WorldSettings
{
    public ulong Seed = 234;
    public double ContinentRatio = 0.4;
    public double PlateMergeRatio = 0.13;
    public double MaxTectonicMovement = 10.0;
    public double MaxAltitude = 2000.0;

    public Rect2 Bounds = new Rect2(-5000, -5000, 10000, 10000);
    public double MinimumCellDistance = 50;
    public double AltitudePropagationDecrement = 0.8;
    public double AltitudePropagationSharpness = 0.1;


    public WorldSettings(ulong seed = 234)
    {
        Seed = seed == 0 ? GD.Randi() : seed;
    }
}
