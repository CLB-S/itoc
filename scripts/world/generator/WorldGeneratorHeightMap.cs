using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PatternSystem;

namespace WorldGenerator;

public partial class WorldGenerator
{
    #region Loop subdivision 
    private readonly ConcurrentDictionary<(int, int), Vector2> _edgeMidpoints = new();
    private readonly ConcurrentDictionary<(int, int), double> _edgeMidpointHeights = new();
    private readonly ConcurrentDictionary<int, double> _adjustedVertexHeights = new();

    private double GetAdjustedVertexHeight(int vertexIndex)
    {
        if (_adjustedVertexHeights.TryGetValue(vertexIndex, out double adjustedHeight))
            return adjustedHeight;

        // Get neighbors of this vertex to calculate the adjusted height
        var neighbors = GetNeighborCellIndices(vertexIndex).ToList();
        int n = neighbors.Count;

        if (n <= 2)
        {
            // For vertices with very few neighbors, keep original height
            _adjustedVertexHeights[vertexIndex] = CellDatas[vertexIndex].Height;
            return CellDatas[vertexIndex].Height;
        }

        // Loop subdivision weight formula: (1-n*beta) * original + beta * sum(neighbors)
        // where beta = 1/n * (5/8 - (3/8 + 1/4 * cos(2*PI/n))^2)
        double beta;
        if (n == 3)
            beta = 3.0 / 16.0;
        else
            beta = 3.0 / (8.0 * n);

        // Calculate the sum of neighbor heights
        double neighborSum = 0;
        foreach (var neighbor in neighbors)
            neighborSum += CellDatas[neighbor].Height;

        // Calculate the adjusted height using Loop formula
        double originalHeight = CellDatas[vertexIndex].Height;
        double newHeight = (1 - n * beta) * originalHeight + beta * neighborSum;

        _adjustedVertexHeights[vertexIndex] = newHeight;
        return newHeight;
    }

    private (Vector2, double) GetOrCreateEdgeMidpoint(int i, int j)
    {
        // Ensure i < j for consistent dictionary keys
        if (i > j)
            (i, j) = (j, i);

        var key = (i, j);

        if (_edgeMidpoints.TryGetValue(key, out Vector2 midpoint))
            return (midpoint, _edgeMidpointHeights[key]);
        else
        {
            // Calculate the midpoint position
            midpoint = (SamplePoints[i] + SamplePoints[j]) * 0.5f;
            _edgeMidpoints[key] = midpoint;

            // Calculate the midpoint height using Loop subdivision rules
            // For edge midpoints, we use 1/2 of each endpoint
            var height = (CellDatas[i].Height + CellDatas[j].Height) * 0.5;
            _edgeMidpointHeights[key] = height;

            return (midpoint, height);
        }
    }

    public (Vector2[], double[]) SubdivideTriangle(int i0, int i1, int i2)
    {
        // Get original triangle vertices and heights
        var p0 = SamplePoints[i0];
        var p1 = SamplePoints[i1];
        var p2 = SamplePoints[i2];

        var h0 = GetAdjustedVertexHeight(i0);
        var h1 = GetAdjustedVertexHeight(i1);
        var h2 = GetAdjustedVertexHeight(i2);

        // Get or create edge midpoints
        var (e01, h01) = GetOrCreateEdgeMidpoint(i0, i1);
        var (e12, h12) = GetOrCreateEdgeMidpoint(i1, i2);
        var (e20, h20) = GetOrCreateEdgeMidpoint(i2, i0);

        // Return all points and heights of the subdivided triangle
        // Original vertices + edge midpoints
        return (
            [p0, p1, p2, e01, e12, e20],
            [h0, h1, h2, h01, h12, h20]
        );
    }

    public (Vector2, Vector2, Vector2, double, double, double) GetSubdividedTriangleContainingPoint(Vector2 point, int i0, int i1, int i2)
    {
        var (points, heights) = SubdivideTriangle(i0, i1, i2);

        // Original vertices
        var p0 = points[0];
        var p1 = points[1];
        var p2 = points[2];

        // Edge midpoints
        var e01 = points[3];
        var e12 = points[4];
        var e20 = points[5];

        // Heights
        var h0 = heights[0];
        var h1 = heights[1];
        var h2 = heights[2];
        var h01 = heights[3];
        var h12 = heights[4];
        var h20 = heights[5];

        // Check which of the four subdivided triangles contains the point
        if (GeometryUtils.IsPointInTriangle(point, p0, e01, e20))
            return (p0, e01, e20, h0, h01, h20);

        if (GeometryUtils.IsPointInTriangle(point, e01, p1, e12))
            return (e01, p1, e12, h01, h1, h12);

        if (GeometryUtils.IsPointInTriangle(point, e20, e12, p2))
            return (e20, e12, p2, h20, h12, h2);

        if (GeometryUtils.IsPointInTriangle(point, e01, e12, e20))
            return (e01, e12, e20, h01, h12, h20);

        // Fallback - should not happen if the point is in the original triangle
        GD.PrintErr("Point is not in any subdivided triangle.");
        return (p0, p1, p2, h0, h1, h2);
    }
    #endregion

    protected void InitIdwInterpolator()
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

    protected virtual double NoiseOverlay(double x, double y)
    {
        return 0; //_heightPattern.Evaluate(x, y);
    }

    private static Vector2 Warp(Vector2 point, PatternTreeNode pattern)
    {
        var warpedPoint = new Vector2(point.X, point.Y);
        warpedPoint.X += pattern.Evaluate(warpedPoint.X, warpedPoint.Y);
        warpedPoint.Y += pattern.Evaluate(warpedPoint.Y, warpedPoint.X);
        return warpedPoint;
    }

    public double GetRawHeight(double x, double y, bool loopDivision = true, bool domainWarping = false, bool noiseOverlay = false)
    {
        var point = new Vector2(x, y);
        if (domainWarping)
            point = Warp(point, _domainWarpPattern);

        var (i0, i1, i2) = GetTriangleContainingPoint(point);

        double height;
        if (loopDivision)
        {
            var (p0, p1, p2, h0, h1, h2) = GetSubdividedTriangleContainingPoint(point, i0, i1, i2);
            height = LinearInterpolator.Interpolate(p0, p1, p2, h0, h1, h2, point);
        }
        else
        {
            var p0 = SamplePoints[i0];
            var p1 = SamplePoints[i1];
            var p2 = SamplePoints[i2];

            height = LinearInterpolator.Interpolate(p0, p1, p2,
                CellDatas[i0].Height, CellDatas[i1].Height, CellDatas[i2].Height,
                new Vector2(x, y));
        }

        if (noiseOverlay)
            height += NoiseOverlay(x, y);
        return height;
    }

    public double[,] CalculateChunkHeightMap(Vector2I chunkPos)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        var rect = new Rect2I(chunkPos * ChunkMesher.CS, ChunkMesher.CS, ChunkMesher.CS);
        return HeightMapUtils.ConstructChunkHeightMap(rect, (x, y) => GetRawHeight(x, y, true, true, true), 2);
    }

    public double[,] CalculateHeightMap(int resolutionX, int resolutionY, Rect2I bounds, bool parallel = false,
        int upscaleLevel = 2)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        return HeightMapUtils.ConstructHeightMap(resolutionX, resolutionY, bounds, (x, y) => GetRawHeight(x, y),
            parallel, upscaleLevel);
    }

    public double[,] CalculateFullHeightMap(int resolutionX, int resolutionY)
    {
        return CalculateHeightMap(resolutionX, resolutionY, Settings.Bounds, true);
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