using Godot;

public class WorldSettings
{
    #region General Settings

    public ulong Seed = 234;
    public Rect2I Bounds = new(-20000, -20000, 40000, 40000);
    public Vector2 WorldCenter => Bounds.Position + Bounds.Size / 2;
    public int PoisosonDiskSamplingIterations = 8;
    public double NormalizedMinimumCellDistance { get; set; } = 0.8;

    public double MinimumCellDistance
    {
        get => NormalizedMinimumCellDistance * Bounds.Size.Y / 200.0;
        set => NormalizedMinimumCellDistance = value * 200.0f / Bounds.Size.Y;
    }

    public double NormalizedNoiseFrequency { get; set; } = 0.8;

    public double NoiseFrequency
    {
        get => NormalizedNoiseFrequency / 4000.0;
        set => NormalizedNoiseFrequency = value * 4000.0;
    }

    public double UpliftNoiseFrequency { get; set; } = 0.7;
    public double UpliftNoiseIntensity { get; set; } = -0.3;


    #endregion

    #region Tectonic Settings

    public double ContinentRatio = 0.8;
    public double PlateMergeRatio = 0.0;
    public double MaxTectonicMovement = 10.0;
    public double MaxUplift = 1300.0;
    public double UpliftPropagationDecrement = 0.8;
    public double UpliftPropagationSharpness = 0.0;

    #endregion

    #region Fluvial Erosion Settings

    public double ErosionRate = 5;
    public double ErosionTimeStep = 0.1;
    public double ErosionConvergenceThreshold = 2.0;
    public int MaxErosionIterations = 150;

    #endregion

    #region Orbital Settings

    public double OrbitalInclinationAngle = 20.0;
    public double OrbitalRevolutionDays = 64.0;
    public double MinutesPerDay = 30.0;

    #endregion

    #region Climate Settings

    public double EquatorialTemperature = 40.0;
    public double PolarTemperature = -30.0;
    public double MaxPrecipitation = 1.703;

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