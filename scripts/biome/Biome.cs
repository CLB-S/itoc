using System;
using Godot;

public class Biome : IEquatable<Biome>
{
    public string Id { get; }
    public double MinTemperature { get; }
    public double MaxTemperature { get; }
    public double MinPrecipitation { get; }
    public double MaxPrecipitation { get; }
    public double MinHeight { get; }
    public double MaxHeight { get; }
    public Color Color { get; private set; }

    public Biome(string id, double minTemp, double maxTemp, double minPrecip, double maxPrecip,
        double minHeight, double maxHeight, Color color)
    {
        Id = id;
        MinTemperature = minTemp;
        MaxTemperature = maxTemp;
        MinPrecipitation = minPrecip;
        MaxPrecipitation = maxPrecip;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
        Color = color;
    }

    public bool MatchesConditions(double temperature, double precipitation, double height)
    {
        return temperature >= MinTemperature && temperature <= MaxTemperature &&
               precipitation >= MinPrecipitation && precipitation <= MaxPrecipitation &&
               height >= MinHeight && height <= MaxHeight;
    }

    public bool Equals(Biome other)
    {
        return other.Id == Id;
    }
}