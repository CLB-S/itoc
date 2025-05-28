using Godot;
using ITOC.Core.PatternSystem;

namespace ITOC.Core;

// TODO: Denpendency checking.
public class PatternLibrary
{
    private static PatternLibrary _instance;
    private readonly Dictionary<string, PatternTree> _patterns = new();

    public static PatternLibrary Instance => _instance ??= new PatternLibrary();

    private PatternLibrary()
    {
        RegisterBuiltInPatterns();
        LoadPatterns();
    }

    public PatternLibrary(int seed)
    {
        RegisterBuiltInPatterns();
        LoadPatterns();
        foreach (var pattern in _patterns.Values)
            pattern.SetSeed(seed);
    }

    public PatternTree GetPattern(string patternId)
    {
        if (_patterns.TryGetValue(patternId, out var pattern))
            return pattern;

        GD.PrintErr($"Pattern with ID {patternId} not found.");
        return null;
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
                NoiseType = NoiseType.Simplex,
                Frequency = 0.002,
                FractalType = FractalType.Ridged,
                FractalOctaves = 1
            })
            .Multiply(new FastNoiseLiteNode(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.SimplexSmooth,
                FractalType = FractalType.None,
                Frequency = 0.0015
            }).Max(-0.6).Add(0.8))
            .Add(new FastNoiseLiteNode(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Simplex,
                Frequency = 0.004,
                FractalOctaves = 4,
                DomainWarpEnabled = true,
                DomainWarpAmplitude = 150,
                DomainWarpFractalType = DomainWarpFractalType.None,
                DomainWarpFrequency = 0.002
            }))
            .Multiply(30)
            .Build());

        RegisterPattern(new PatternTreeBuilder("hill", "Hill")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Simplex,
                Frequency = 0.004,
                FractalOctaves = 4,
                DomainWarpEnabled = true,
                DomainWarpAmplitude = 150,
                DomainWarpFractalType = DomainWarpFractalType.None,
                DomainWarpFrequency = 0.002
            })
            .Multiply(20)
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
            .Multiply(10)
            .Build());
    }
}