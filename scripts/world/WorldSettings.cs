using Godot;
using System;

public class WorldSettings
{
    private float _minimumCellDistance = 1.0f;
    private float _noiseFrequency = 1.0f;

    public ulong Seed = 234;

    #region Generation Settings
    public float ContinentRatio = 0.6f;
    public float PlateMergeRatio = 0.13f;
    public float MaxTectonicMovement = 10.0f;
    public float MaxAltitude = 1000.0f;

    public Rect2I Bounds = new Rect2I(-20000, -20000, 40000, 40000);
    public float MinimumCellDistance { get => _minimumCellDistance * Bounds.Size.Y / 200.0f; set => _minimumCellDistance = value; }
    public float NoiseFrequency { get => _noiseFrequency * Bounds.Size.Y / 4000.0f; set => _noiseFrequency = value; }

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
