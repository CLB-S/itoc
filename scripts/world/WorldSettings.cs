using Godot;
using System;

public class WorldSettings
{
    #region General Settings
    public ulong Seed = 234;
    public Rect2I Bounds = new Rect2I(-20000, -20000, 40000, 40000);
    public Vector2 WorldCenter { get => Bounds.Position + Bounds.Size / 2; }
    public int PoisosonDiskSamplingIterations = 8;
    public float NormalizedMinimumCellDistance { get; set; } = 0.8f;
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
    #endregion

    #region Tectonic Settings
    public float ContinentRatio = 0.6f;
    public float PlateMergeRatio = 0.13f;
    public float MaxTectonicMovement = 10.0f;
    public float MaxUplift = 1300.0f;
    public float UpliftPropagationDecrement = 0.8f;
    public float UpliftPropagationSharpness = 0.0f;
    #endregion

    #region Fluvial Erosion Settings
    public float ErosionRate = 5f;
    public float ErosionTimeStep = 0.1f;
    public float ErosionConvergenceThreshold = 2.0f;
    public int MaxErosionIterations = 150;
    #endregion

    #region Orbital Settings
    public float OrbitalInclinationAngle = 20.0f;
    public float OrbitalRevolutionDays = 64.0f;
    public float MinutesPerDay = 30.0f;
    #endregion


    public WorldSettings(ulong seed = 234)
    {
        Seed = seed == 0 ? GD.Randi() : seed;
    }

    public WorldSettings Clone()
    {
        return (WorldSettings)MemberwiseClone();
    }
}
