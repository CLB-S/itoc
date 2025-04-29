using System;
using System.Collections.Generic;
using Godot;

namespace PatternSystem;

// TODO: Denpendency checking.
public class PatternLibrary
{
    private static PatternLibrary _instance;
    private Dictionary<string, PatternTree> _patterns = new();

    public static PatternLibrary Instance => _instance ??= new PatternLibrary();

    private PatternLibrary()
    {
        RegisterBuiltInPatterns();
        LoadPatterns();
    }

    public PatternTree GetPattern(string patternId)
    {
        return _patterns.TryGetValue(patternId, out var pattern) ? pattern : null;
    }

    public void RegisterPattern(PatternTree pattern)
    {
        if (_patterns.ContainsKey(pattern.Id))
            throw new ArgumentException($"Pattern with name {pattern.Id} already exists.");

        _patterns[pattern.Id] = pattern;
    }

    private void LoadPatterns()
    {
        // TODO
    }

    private void RegisterBuiltInPatterns()
    {
        RegisterPattern(new PatternTreeBuilder("mountain", "Mountain With Ridges")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = 233,
                NoiseType = NoiseType.Simplex,
                Frequency = 0.002,
                FractalType = FractalType.Ridged,
                FractalOctaves = 1,
            })
            .Multiply(new FastNoiseLiteNode(new FastNoiseLiteSettings
            {
                Seed = 2332,
                NoiseType = NoiseType.SimplexSmooth,
                FractalType = FractalType.None,
                Frequency = 0.0015,
            }).Max(-0.6).Add(0.8))
            .Add(new FastNoiseLiteNode(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Simplex,
                Frequency = 0.004,
                FractalOctaves = 4,
                DomainWarpEnabled = true,
                DomainWarpAmplitude = 150,
                DomainWarpFractalType = DomainWarpFractalType.None,
                DomainWarpFrequency = 0.002,
            }))
            .Multiply(30)
            .Add(50)
            .Build());

        RegisterPattern(new PatternTreeBuilder("plain", "Plain")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Perlin,
                // Frequency = 0.01,
                FractalOctaves = 2
            })
            .Multiply(Mathf.Pi / 2)
            .ApplyOperation(SingleOperationType.Sin)
            .Multiply(6)
            .Add(30)
            .Build());

    }
}