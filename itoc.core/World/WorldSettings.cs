using Godot;

namespace ITOC.Core;

public class WorldSettings
{
    #region General Settings

    public ulong Seed = 234;
    public Vector2 WorldCenter => Vector2.Zero;

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