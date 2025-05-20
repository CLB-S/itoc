using Godot;

// TODO: Seperating world settings and world generator settings.

public class WorldSettings
{
    #region General Settings

    public ulong Seed = 234;
    public Rect2I Bounds = new(-50000, -50000, 100000, 100000);
    public Vector2 WorldCenter => Bounds.Position + Bounds.Size / 2;
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

    #region Orbital Settings

    public double OrbitalInclinationAngle = 20.0;
    public double OrbitalRevolutionDays = 64.0;
    public double MinutesPerDay = 30.0;

    #endregion

    #region Climate Settings

    public double EquatorialTemperature = 35.0;
    public double PolarTemperature = -50;
    public double MaxPrecipitation = 1.3555;
    public double TemperatureGradientWithAltitude = 2 / 100.0; // Higher then reality.

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