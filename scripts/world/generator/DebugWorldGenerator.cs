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
        _debugHeightPattern = PatternLibrary.Instance.GetPattern("plain");
    }

    protected override double NoiseOverlay(double x, double y)
    {
        return _debugHeightPattern.Evaluate(x, y);
    }

    protected override void InitializePipeline()
    {
        _generationPipeline.AddLast(new GenerationStep(GenerationState.Initializing, InitializeResources));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.GeneratingSamplePoints, GenerateSamplePoints));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.InitializingCellDatas, InitializeCellDatas));

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