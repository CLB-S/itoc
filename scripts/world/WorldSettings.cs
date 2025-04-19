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

    public Rect2I Bounds = new Rect2I(-20000, -20000, 40000, 40000);
    public int PoisosonDiskSamplingIterations = 8;
    public float NormalizedMinimumCellDistance { get; set; } = 1.0f;
    public float MinimumCellDistance
    {
        get => NormalizedMinimumCellDistance * Bounds.Size.Y / 200.0f;
        set => NormalizedMinimumCellDistance = value * 200.0f / Bounds.Size.Y;
    }

    public float NormalizedNoiseFrequency { get; set; } = 1.0f;
    public float NoiseFrequency
    {
        get => NormalizedNoiseFrequency / 4000.0f;
        set => NormalizedNoiseFrequency = value * 4000.0f;
    }


    public float AltitudePropagationDecrement = 0.8f;
    public float AltitudePropagationSharpness = 0.1f;
    #endregion

    // Fluvial erosion settings
    public float ErosionRate = 5f;
    public float TimeStep = 0.1f;
    public float ErosionConvergenceThreshold = 0.02f;
    public int MaxErosionIterations = 150;

    public WorldSettings(ulong seed = 234)
    {
        Seed = seed == 0 ? GD.Randi() : seed;
    }

    public WorldSettings Clone()
    {
        return (WorldSettings)MemberwiseClone();
    }
}
