using Godot;
using System;

public class WorldSettings
{
    public ulong Seed = 234;

    #region Generation Settings
    public float ContinentRatio = 0.6f;
    public float PlateMergeRatio = 0.13f;
    public float MaxTectonicMovement = 10.0f;
    public float MaxAltitude = 1000.0f;

    public Rect2I Bounds = new Rect2I(-10000, -10000, 20000, 20000);
    public float MinimumCellDistance = 50;
    public float AltitudePropagationDecrement = 0.8f;
    public float AltitudePropagationSharpness = 0.1f;
    public float NoiseFrequency = 20.0f;
    #endregion

    // Fluvial erosion settings
    public float ErosionRate { get; set; } = 5f;
    public float TimeStep { get; set; } = 0.1f;
    public float ErosionConvergenceThreshold { get; set; } = 0.02f;
    public int MaxErosionIterations { get; set; } = 200;

    public WorldSettings(ulong seed = 234)
    {
        Seed = seed == 0 ? GD.Randi() : seed;
    }

    public WorldSettings Clone()
    {
        return (WorldSettings)MemberwiseClone();
    }
}
