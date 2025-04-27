using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using DelaunatorSharp;
using Godot;
using PatternSystem;
using YamlDotNet.Serialization;

namespace WorldGenerator;

// TODO: World without sample points.
public class DebugWorldGenerator : WorldGenerator
{
    private PatternTreeNode _debugHeightPattern;

    public DebugWorldGenerator(WorldSettings settings) : base(settings)
    {
        _debugHeightPattern = new PatternTreeBuilder("debug_tree", "Debug Tree ⭐")
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
            .ApplyOperation(SingleOperationType.Sin)
            .Multiply(50)
            .Add(new FastNoiseLiteNode(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Perlin,
                // Frequency = 0.01,
                FractalOctaves = 4
            }).Multiply(40))
            .Add(80)
            .Build();
    }

    protected override double NoiseOverlay(double x, double y)
    {
        return _debugHeightPattern.Evaluate(x, y);
    }

    protected override void InitializePipeline()
    {
        _generationPipeline.AddLast(new GenerationStep(GenerationState.Initializing, InitializeResources));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.GeneratingPoints, GeneratePoints));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.CreatingVoronoi, CreateVoronoiDiagram));

        // _generationPipeline.AddLast(new GenerationStep(GenerationState.Custom, ApplyHeight));

        _generationPipeline.AddLast(new GenerationStep(GenerationState.InitInterpolator, InitInterpolator));
    }

    private void SetHeightForSamplePoints()
    {
        foreach (var (i, cell) in _cellDatas)
        {
            var pos = SamplePoints[i];
            cell.Height = _debugHeightPattern.Evaluate(pos.X, pos.Y);
        }
    }

}