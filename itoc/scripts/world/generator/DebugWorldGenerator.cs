using System;
using Godot;
using PatternSystem;

namespace ITOC.WorldGeneration;


// TODO: World without sample points.
public class DebugWorldGenerator : WorldGenerator
{
    private readonly PatternTreeNode _debugHeightPattern;
    private readonly PatternTreeNode _debugHeightPattern1;

    public DebugWorldGenerator(WorldSettings settings) : base(settings)
    {
        _debugHeightPattern = PatternLibrary.Instance.GetPattern("mountain");
        _debugHeightPattern1 = PatternLibrary.Instance.GetPattern("plain");
    }

    protected override double NoiseOverlay(double x, double y)
    {
        var weight = Mathf.Clamp(x, 0, 200) / 200.0;
        return MergePatterns(x, y, _debugHeightPattern, _debugHeightPattern1, weight);
    }

    /// <summary>
    ///     Merges two patterns based on a weight parameter.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="pattern1">First pattern</param>
    /// <param name="pattern2">Second pattern</param>
    /// <param name="weight">Weight of the first pattern (0.0 to 1.0)</param>
    /// <returns>Merged value from both patterns</returns>
    private double MergePatterns(double x, double y, PatternTreeNode pattern1, PatternTreeNode pattern2, double weight)
    {
        // Ensure weight is between 0 and 1
        weight = Mathf.Clamp((float)weight, 0.0f, 1.0f);

        // If weight is at extremes, only evaluate the necessary pattern
        if (weight <= 0.0)
            return pattern2.Evaluate(x, y);
        if (weight >= 1.0)
            return pattern1.Evaluate(x, y);

        // Otherwise, evaluate both patterns and blend them
        return pattern1.Evaluate(x, y) * weight + pattern2.Evaluate(x, y) * (1.0 - weight);
    }


    public override double[,] CalculateChunkHeightMap(Vector2I chunkColumnPos, Func<double, double, double> getHeight)
    {
        if (State != WorldGenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        var rect = new Rect2I(chunkColumnPos * ChunkMesher.CS, ChunkMesher.CS, ChunkMesher.CS);
        return HeightMapUtils.ConstructChunkHeightMap(rect, (x, y) => 10, 2);
    }
}