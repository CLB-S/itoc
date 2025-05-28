using Godot;

namespace ITOC.Core.Utils;

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
        return Color.FromHsv((float)rng.Randf(), (float)rng.RandfRange(0.2f, 0.6f), (float)rng.RandfRange(0.9f, 1.0f));
    }

    /// <summary>
    ///     Color palette for height maps.
    /// </summary>
    /// <param name="height">[-1, 1]</param>
    /// <returns></returns>
    public static Color GetHeightColor(float height, float seaLevel = 0f)
    {
        // Define color stops
        var mountain = new Color(0.5f, 0.4f, 0.3f);
        var snow = new Color(1.0f, 1.0f, 1.0f);

        // Interpolate between colors
        if (height < seaLevel)
            return new Color(0, 0, 1 + height);
        if (height < seaLevel + 0.03f)
            return new Color(0.9f, 0.8f, 0.6f); // sand
        if (height < seaLevel + 0.4f)
            return new Color(0.2f - height, 0.6f - height, 0.2f - height); // grass
        if (height < seaLevel + 0.8f)
            return new Color(0.6f - height * 0.5f, 0.5f - height * 0.5f, 0.4f - height * 0.5f); // mountain
        return mountain.Lerp(snow, (height - (seaLevel + 0.3f)) / 0.7f);
    }
}