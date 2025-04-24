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
        // var math = new MathExpressionNode("30 * sin((0.03 * Px)^2 + (0.03 * Py)^2)");

        _debugHeightPattern = new PatternTreeBuilder("debug_tree", "Debug Tree ⭐")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                NoiseType = NoiseType.Cellular,
                Frequency = 0.02,
                FractalType = FractalType.None
            })
            .ApplyMathExpression("50 * sin(0.01 * Px + 0.01 * Py) * x + 30")
            .Build();

        GD.Print(PatternTreeJsonConverter.Serialize(_debugHeightPattern));

        // new PatternTreeJsonExample();
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