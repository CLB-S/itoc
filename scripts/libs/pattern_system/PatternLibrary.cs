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
        RegisterPattern(new PatternTreeBuilder("mountain", "Mountain")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Simplex,
                Frequency = 0.001,
                FractalOctaves = 3,
                FractalType = FractalType.Ridged,
                FractalGain = 0.8,
                FractalWeightedStrength = 0.4
            })
            .Max(-0.3)
            .Multiply(Mathf.Pi / 2)
            .ApplyOperation(SingleOperationType.Sin) // Make peaks smoother.
            .Multiply(50)
            .Add(new FastNoiseLiteNode(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Perlin,
                FractalOctaves = 4
            }).Multiply(40))
            .Add(80)
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