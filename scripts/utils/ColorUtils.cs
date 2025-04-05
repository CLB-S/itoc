using Godot;

public static class ColorUtils
{
    public static Color RandomColorHSV()
    {
        return Color.FromHsv(GD.Randf(), (float)GD.RandRange(0.2, 0.6), (float)GD.RandRange(0.9, 1.0));
    }

    public static Color RandomColorHSV(ulong seed)
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = seed;
        return Color.FromHsv(rng.Randf(), rng.RandfRange(0.2f, 0.6f), rng.RandfRange(0.9f, 1.0f));
    }

    /// <summary>
    /// Color palette for height maps.
    /// </summary>
    /// <param name="height">[-1, 1]</param>
    /// <returns></returns>
    public static Color GetSmoothHeightColor(float height, float seaLevel = 0f)
    {
        // Define color stops
        var deepWater = new Color(0.0f, 0.0f, 0.3f);
        var shallowWater = new Color(0.2f, 0.2f, 0.8f);
        var sand = new Color(0.9f, 0.8f, 0.6f);
        var grass = new Color(0.2f, 0.6f, 0.2f);
        var mountain = new Color(0.5f, 0.4f, 0.3f);
        var snow = new Color(1.0f, 1.0f, 1.0f);

        // Normalize height relative to sea level
        float normalizedHeight = Mathf.Clamp((height - seaLevel + 0.1f) / (1.0f - seaLevel + 0.1f), 0f, 1f);

        // Interpolate between colors
        if (height < seaLevel - 0.1f)
        {
            return deepWater.Lerp(shallowWater, (height + 0.1f) / (seaLevel - 0.1f + 0.1f));
        }
        else if (height < seaLevel)
        {
            return shallowWater.Lerp(sand, (height - (seaLevel - 0.1f)) / 0.1f);
        }
        else if (height < seaLevel + 0.1f)
        {
            return sand.Lerp(grass, (height - seaLevel) / 0.1f);
        }
        else if (height < seaLevel + 0.3f)
        {
            return grass.Lerp(mountain, (height - (seaLevel + 0.1f)) / 0.2f);
        }
        else
        {
            return mountain.Lerp(snow, (height - (seaLevel + 0.3f)) / 0.7f);
        }
    }
}