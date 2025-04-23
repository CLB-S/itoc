using Godot;

public static class OrbitalUtils
{
    /// <summary>
    ///     Calculates the local time in seconds。
    /// </summary>
    /// <param name="longitude"> The longitude in degrees. [-180 180]</param>
    public static double LocalTime(double currentTimeSeconds, double longitude, double minutesPerDay)
    {
        var T_day = minutesPerDay * 60;
        var timeInDay = currentTimeSeconds % T_day;

        var longitudeOffset = longitude / 360.0 * T_day;
        var localTimeSeconds = (timeInDay + longitudeOffset) % T_day;
        if (localTimeSeconds < 0)
            localTimeSeconds += T_day;

        return localTimeSeconds;
    }

    /// <summary>
    ///     Calculates the solar elevation and azimuth angles.
    /// </summary>
    /// <param name="latitude"> The latitude in degrees. [-90 90]</param>
    /// <param name="longitude"> The longitude in degrees. [-180 180]</param>
    /// <returns> The solar elevation and azimuth angles in degrees.</returns>
    public static (double SolarElevation, double SolarAzimuth) CalculateSunPosition(double currentTimeSeconds,
        double latitude, double longitude, double orbitalInclinationAngle, double orbitalRevolutionDays,
        double minutesPerDay)
    {
        var T_orbital = orbitalRevolutionDays * minutesPerDay * 60;
        var T_day = minutesPerDay * 60;

        var orbitalAngle = 2 * Mathf.Pi * currentTimeSeconds / T_orbital;
        var delta_deg = orbitalInclinationAngle * Mathf.Sin(orbitalAngle);
        var delta_rad = delta_deg * Mathf.Pi / 180;

        var localTimeSeconds = LocalTime(currentTimeSeconds, longitude, minutesPerDay);

        var H_deg = (localTimeSeconds - T_day / 2.0) * (360.0 / T_day);
        var H_rad = H_deg * Mathf.Pi / 180;

        var lat_rad = latitude * Mathf.Pi / 180;

        var sin_h = Mathf.Sin(lat_rad) * Mathf.Sin(delta_rad) +
                    Mathf.Cos(lat_rad) * Mathf.Cos(delta_rad) * Mathf.Cos(H_rad);
        sin_h = Mathf.Clamp(sin_h, -1.0, 1.0);
        var h_rad = Mathf.Asin(sin_h);
        var solarElevation = h_rad * 180 / Mathf.Pi;

        double solarAzimuth = 0;
        var cos_h = Mathf.Cos(h_rad);
        if (Mathf.Abs(cos_h) >= 1e-6)
        {
            var cos_A = Mathf.Sin(delta_rad) * Mathf.Cos(lat_rad) -
                        Mathf.Cos(delta_rad) * Mathf.Sin(lat_rad) * Mathf.Cos(H_rad);
            cos_A /= cos_h;
            var sin_A = -Mathf.Cos(delta_rad) * Mathf.Sin(H_rad) / cos_h;
            var A_rad = Mathf.Atan2(sin_A, cos_A);
            solarAzimuth = A_rad * 180 / Mathf.Pi;
            if (solarAzimuth < 0)
                solarAzimuth += 360;
        }

        return (solarElevation, solarAzimuth);
    }

    /// <summary>
    ///     Calculates the sunrise and sunset times in seconds.
    /// </summary>
    /// <param name="latitude"> The latitude in degrees. [-90 90]</param>
    public static (double? SunriseTime, double? SunsetTime) CalculateSunriseSunset(double currentTimeSeconds,
        double latitude, double orbitalInclinationAngle, double orbitalRevolutionDays, double minutesPerDay)
    {
        var T_orbital = orbitalRevolutionDays * minutesPerDay * 60;
        var T_day = minutesPerDay * 60;

        var orbitalAngle = 2 * Mathf.Pi * currentTimeSeconds / T_orbital;
        var delta_deg = orbitalInclinationAngle * Mathf.Sin(orbitalAngle);
        var delta_rad = delta_deg * Mathf.Pi / 180;

        double? sunriseTime = null;
        double? sunsetTime = null;
        var tan_lat = Mathf.Tan(latitude * Mathf.Pi / 180);
        var tan_delta = Mathf.Tan(delta_rad);
        var value = tan_lat * tan_delta;

        if (Mathf.Abs(value) <= 1)
        {
            var H0_rad = Mathf.Acos(-value);
            var H0_deg = H0_rad * 180 / Mathf.Pi;

            var H_rise = -H0_deg;
            var H_set = H0_deg;

            sunriseTime = Mod(H_rise * T_day / 360 + T_day / 2, T_day);
            sunsetTime = Mod(H_set * T_day / 360 + T_day / 2, T_day);
        }

        return (sunriseTime, sunsetTime);
    }

    private static double Mod(double x, double m)
    {
        return (x % m + m) % m;
    }
}