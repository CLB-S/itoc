using Godot;

namespace ITOC.Core.Utils;

public class WindSettings
{
    public double TradeWindMagnitude = 0.8;
    public double WesterliesMagnitude = 1.2;
    public double PolarMagnitude = 0.5;
    public double DoldrumsMagnitude = 0.1;
    public double DoldrumsBandSize = 5;
    public double HorseLatitudesMagnitude = 0.2;
    public double HorseWindBandSize = 5;
    public double LongitudeVariationMagnitude = 10;
}

public static class ClimateUtils
{
    /// <summary>
    ///     P(φ) = P_max [exp{-(φ/a)^2} + b * exp{-((|φ|-60°)/c)^2}]
    /// </summary>
    public static double GetPrecipitation(
        double latitudeDegrees,
        double maxPrecipitation,
        double a = 0.95,
        double b = 0.45,
        double c = 0.45
    )
    {
        var latitude = Mathf.DegToRad(latitudeDegrees);
        var pt = Mathf.Exp(-(latitude / a) * (latitude / a));
        var x = (Mathf.Abs(latitude) - Mathf.Pi / 3.0) / c;
        var pc = Mathf.Exp(-x * x);
        return maxPrecipitation * (pt + b * pc);
    }

    public static double GetTemperature(
        double latitudeDegrees,
        double equatorialTemperature,
        double polarTemperature
    )
    {
        var latitude = Mathf.DegToRad(latitudeDegrees);
        var dt = (equatorialTemperature - polarTemperature * 2 / Mathf.Pi) / 2;
        var t = equatorialTemperature - dt;
        return t
            + dt
                * (
                    Mathf.Abs(2 * latitude) < Mathf.Pi / 2
                        ? Mathf.Cos(2 * latitude)
                        : Mathf.Pi / 2 - Mathf.Abs(2 * latitude)
                );
    }

    public static Vector2 GetSurfaceWind(
        double latitude,
        double longitude,
        WindSettings settings = null,
        RandomNumberGenerator rng = null,
        double seasonalOffset = 0
    )
    {
        settings ??= new WindSettings();
        rng ??= new RandomNumberGenerator();

        // Adjust latitude for seasonal variations
        var adjustedLat = latitude - seasonalOffset;

        // Determine base wind properties
        double baseAngle;
        double baseMagnitude;

        if (Mathf.Abs(adjustedLat) < settings.DoldrumsBandSize * 0.5)
        {
            // Doldrums (equatorial low-pressure)
            baseAngle = rng.Randf() * 360.0;
            baseMagnitude = settings.DoldrumsMagnitude;
        }
        else if (Mathf.Abs(Mathf.Abs(adjustedLat) - 30) < settings.HorseWindBandSize * 0.5)
        {
            // Horse latitudes (subtropical high-pressure)
            baseAngle = rng.Randf() * 360.0;
            baseMagnitude = settings.HorseLatitudesMagnitude;
        }
        else
        {
            // Main circulation cells
            if (Mathf.Abs(adjustedLat) < 30)
            {
                // Trade winds
                baseMagnitude = settings.TradeWindMagnitude;
                baseAngle = adjustedLat > 0 ? 45 : 135; // NE in NH, SE in SH
            }
            else if (Mathf.Abs(adjustedLat) < 60)
            {
                // Westerlies
                baseMagnitude = settings.WesterliesMagnitude;
                baseAngle = adjustedLat > 0 ? 225 : 315; // SW in NH, NW in SH
            }
            else
            {
                // Polar easterlies
                baseMagnitude = settings.PolarMagnitude;
                baseAngle = 90; // From East in both hemispheres
            }
        }

        // Add longitudinal variation using Perlin noise
        var longitudeEffect =
            Mathf.Sin(Mathf.DegToRad(longitude) * 2) * settings.LongitudeVariationMagnitude; //Mathf.PerlinNoise(longitude / 10f, Time.time) * 2f - 1f;
        baseAngle += longitudeEffect;

        // Apply random noise
        // double noiseAngle = rng.RandfRange(-settings.MaxNoiseAngle, settings.MaxNoiseAngle);
        // double noiseMagnitude = 1f + rng.RandfRange(-settings.MaxNoiseMagnitude, settings.MaxNoiseMagnitude);

        // Calculate final wind vector
        var finalAngle = baseAngle;
        var finalMagnitude = baseMagnitude;

        var finalRad = Mathf.DegToRad(finalAngle);

        return new Vector2(-Mathf.Sin(finalRad), Mathf.Cos(finalRad)) * finalMagnitude;
    }
}
