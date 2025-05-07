using System;
using Godot;
using PatternSystem;

public class Biome : IEquatable<Biome>
{
    public string Id { get; private set; }
    public double MinTemperature { get; private set; }
    public double MaxTemperature { get; private set; }
    public double MinPrecipitation { get; private set; }
    public double MaxPrecipitation { get; private set; }
    public double MinHeight { get; private set; }
    public double MaxHeight { get; private set; }
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