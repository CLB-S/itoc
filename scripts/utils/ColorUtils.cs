using Godot;

public static class ColorUtils
{
    public static Color GetRandomColorHSV()
    {
        return Color.FromHsv(GD.Randf(), (float)GD.RandRange(0.2, 0.6), (float)GD.RandRange(0.9, 1.0));
    }
}