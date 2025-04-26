using System;
using Godot;
using PatternSystem;

public static class ClimateUtils
{
    /// <summary>
    /// P(φ) = P_max [exp{-(φ/a)^2} + b * exp{-((|φ|-60°)/c)^2}]
    /// </summary>
    public static double GetPrecipitation(double latitudeDegrees, double maxPrecipitation, double a = 0.7, double b = 0.4, double c = 0.45)
    {
        var latitude = Mathf.DegToRad(latitudeDegrees);
        var pt = Mathf.Exp(-(latitude / a) * (latitude / a));
        var x = (Mathf.Abs(latitude) - Mathf.Pi / 3.0) / c;
        var pc = Mathf.Exp(-x * x);
        return maxPrecipitation * (pt + b * pc);
    }

    public static double GetTemperature(double latitudeDegrees, double equatorialTemperature, double polarTemperature)
    {
        var latitude = Mathf.DegToRad(latitudeDegrees);
        var t = (equatorialTemperature + polarTemperature) / 2;
        var dt = (equatorialTemperature - polarTemperature) / 2;
        return t + dt * Mathf.Cos(2 * latitude);
    }
}