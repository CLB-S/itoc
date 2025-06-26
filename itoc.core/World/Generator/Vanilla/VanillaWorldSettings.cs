using Godot;

namespace ITOC.Core.WorldGeneration.Vanilla;

public class VanillaWorldSettings : WorldSettings
{
    #region General Settings

    public Rect2I Bounds = new(-50000, -50000, 100000, 100000);
    public new Vector2 WorldCenter => Bounds.Position + Bounds.Size / 2;
    public int PoisosonDiskSamplingIterations = 8;
    public double NormalizedMinimumCellDistance { get; set; } = 0.6;

    public double MinimumCellDistance
    {
        get => NormalizedMinimumCellDistance * Bounds.Size.Y / 200.0;
        set => NormalizedMinimumCellDistance = value * 200.0f / Bounds.Size.Y;
    }

    public double NormalizedNoiseFrequency { get; set; } = 0.8;

    public double NoiseFrequency
    {
        get => NormalizedNoiseFrequency / 10000.0;
        set => NormalizedNoiseFrequency = value * 10000.0;
    }

    public double UpliftNoiseFrequency { get; set; } = 0.7;
    public double UpliftNoiseIntensity { get; set; } = -0.3;

    public double TemperatureNoiseFrequency { get; set; } = 1.0;
    public double TemperatureNoiseIntensity { get; set; } = 10.0;

    public double PrecipitationNoiseFrequency { get; set; } = 0.8;
    public double PrecipitationNoiseIntensity { get; set; } = 0.6;

    public double DomainWarpFrequency { get; set; } = 0.02;
    public double DomainWarpIntensity { get; set; } = 20;

    #endregion

    #region Tectonic Settings

    public double ContinentRatio = 0.8;
    public double PlateMergeRatio = 0.0;
    public double MaxTectonicMovement = 10.0;
    public double MaxUplift = 1000.0;
    public double UpliftPropagationDecrement = 0.8;
    public double UpliftPropagationSharpness = 0.0;

    #endregion

    #region Fluvial Erosion Settings

    public double ErosionRate = 4.5;
    public double ErosionTimeStep = 0.2;
    public double ErosionConvergenceThreshold = 20.0;
    public int MaxErosionIterations = 20;
    public double MaxErosionSlopeAngle = 30.0;

    #endregion
}
