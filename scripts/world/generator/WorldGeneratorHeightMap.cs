using System;
using System.Collections.Generic;
using Godot;

namespace WorldGenerator;

public partial class WorldGenerator
{
    protected void InitInterpolator()
    {
        var posList = new List<Vector2>(_cellDatas.Count);
        var dataList = new List<double>(_cellDatas.Count);
        for (var i = 0; i < _cellDatas.Count; i++)
        {
            posList.Add(_points[i]);
            dataList.Add(_cellDatas[i].Height);
        }

        _heightMapInterpolator = new IdwInterpolator(posList, dataList);
    }

    public double[,] CalculateFullHeightMap(int resolutionX, int resolutionY)
    {
        return CalculateHeightMap(resolutionX, resolutionY, Settings.Bounds, true);
    }

    protected virtual double NoiseOverlay(double x, double y)
    {
        return _heightPattern.Evaluate(x, y);
    }

    public double[,] CalculateChunkHeightMap(Vector2I chunkPos)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        // Consider overlapping edges
        var rect = new Rect2I(chunkPos * ChunkMesher.CS, ChunkMesher.CS_P, ChunkMesher.CS_P);
        return _heightMapInterpolator.ConstructChunkHeightMap(rect, 2, NoiseOverlay);
    }

    public double[,] CalculateHeightMap(int resolutionX, int resolutionY, Rect2I bounds, bool parallel = false,
        int upscaleLevel = 2)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        return _heightMapInterpolator.ConstructHeightMap(resolutionX, resolutionY, bounds, parallel, upscaleLevel);
    }

    public ImageTexture GetFullHeightMapImageTexture(int resolutionX, int resolutionY)
    {
        var heightMap = CalculateFullHeightMap(resolutionX, resolutionY);
        var image = Image.CreateEmpty(resolutionX, resolutionY, false, Image.Format.Rgb8);
        for (var x = 0; x < resolutionX; x++)
            for (var y = 0; y < resolutionY; y++)
            {
                var h = (float)(0.5 * (1 + heightMap[x, y] / MaxHeight));
                image.SetPixel(x, y, new Color(h, h, h));
            }

        return ImageTexture.CreateFromImage(image);
    }
}