using System;
using System.Collections.Generic;
using System.Linq;
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

    protected virtual double GetHeight(double x, double y)
    {
        // return _heightMapInterpolator.GetHeight(x, y);

        var (i0, i1, i2) = GetTriangleContainingPoint(x, y);
        var p0 = SamplePoints[i0];
        var p1 = SamplePoints[i1];
        var p2 = SamplePoints[i2];

        return LinearInterpolator.Interpolate(p0, p1, p2,
            CellDatas[i0].Height, CellDatas[i1].Height, CellDatas[i2].Height,
            new Vector2(x, y));
    }

    public double[,] CalculateChunkHeightMap(Vector2I chunkPos)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        // Consider overlapping edges
        var rect = new Rect2I(chunkPos * ChunkMesher.CS, ChunkMesher.CS_P, ChunkMesher.CS_P);
        return HeightMapUtils.ConstructChunkHeightMap(rect, GetHeight, 2, NoiseOverlay);
    }

    public double[,] CalculateHeightMap(int resolutionX, int resolutionY, Rect2I bounds, bool parallel = false,
        int upscaleLevel = 2)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        return HeightMapUtils.ConstructHeightMap(resolutionX, resolutionY, bounds, GetHeight,
            parallel, upscaleLevel);
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