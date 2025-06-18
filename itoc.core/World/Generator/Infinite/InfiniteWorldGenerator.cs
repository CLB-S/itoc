// deprecated

using Godot;
using ITOC.Core.PatternSystem;
using ITOC.Core.Utils;

namespace ITOC.Core.WorldGeneration.Infinite;

public class InfiniteWorldGenerator : WorldGeneratorBase
{
    private PatternTreeNode _debugHeightPattern;
    private PatternTreeNode _debugHeightPattern1;

    protected override void InitializePipeline()
    {
        _generationPipeline.AddLast(new WorldGenerationStep("initialize", Initialize));
    }

    private void Initialize()
    {
        _debugHeightPattern = PatternLibrary.Instance.GetPattern("mountain");
        _debugHeightPattern1 = PatternLibrary.Instance.GetPattern("plain");
    }

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

    public override ChunkColumn GenerateChunkColumnMetadata(Vector2I chunkColumnIndex)
    {
        // Biome
        var defaultBiome = BiomeLibrary.Instance.GetBiome("plain");
        var size = ChunkColumn.BIOME_MAP_SIZE * ChunkColumn.BIOME_MAP_SIZE;
        var biomes = new PaletteArray<Biome>(size, defaultBiome);

        var chunkColumn = new ChunkColumn(chunkColumnIndex, biomes);

        // Height map
        var getHeight = new Func<double, double, double>((x, y) =>
        {
            var weight = Mathf.Clamp(x, 0, 200) / 200.0;
            return MergePatterns(x, y, _debugHeightPattern, _debugHeightPattern1, weight);
        });

        var heightMap = CalculateChunkHeightMap(chunkColumnIndex, getHeight);
        chunkColumn.SetHeightMap(heightMap);
        return chunkColumn;
    }
}