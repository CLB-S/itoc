using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Supercluster.KDTree;

public class IdwInterpolator
{
    private readonly KDTree<double, float> _kdTree;
    private readonly int _numNeighbors;
    private readonly double _power;

    public IdwInterpolator(IEnumerable<Vector2> positions, IEnumerable<float> heights, double power = 1,
        int numNeighbors = 6)
    {
        var positionsList = positions.ToList();
        var heightsList = heights.ToList();

        if (positionsList.Count != heightsList.Count)
            throw new ArgumentException("Positions and heights must have the same number of elements.");

        var pointsData = positions.Select(p => new[] { p.X, p.Y }).ToArray();

        _kdTree = new KDTree<double, float>(2, pointsData, heights.ToArray(), L2Norm);

        _power = power;
        _numNeighbors = numNeighbors;
    }

    private static double L2Norm(double[] x, double[] y)
    {
        double dist = 0;
        for (var i = 0; i < x.Length; i++) dist += (x[i] - y[i]) * (x[i] - y[i]);

        return dist;
    }

    public float GetHeight(double x, double y)
    {
        var neighbors = _kdTree.NearestNeighbors([x, y], _numNeighbors);

        var totalWeight = 0.0;
        var weightedSum = 0.0;

        foreach (var (pos, height) in neighbors)
        {
            var distance = L2Norm(pos, [x, y]) / 3000;

            // if (distance <= 0) return height;

            var weight = Mathf.Exp(-distance);// 1.0 / Mathf.Pow(distance + 5, _power);
            weightedSum += weight * height;
            totalWeight += weight;
        }

        if (totalWeight <= 0.0)
            // This should not happen unless all weights are zero, which is impossible with distance > 0
            return 0.0f;

        return (float)(weightedSum / totalWeight);
    }

    public float[,] ConstructHeightMap(int resolutionX, int resolutionY, Rect2I rect, bool parallel = false, int upscaleLevel = 2)
    {
        if (upscaleLevel < 0)
            throw new ArgumentException("Upscale level must be non-negative.");

        // Calculate low resolution dimensions
        int lowResX = resolutionX >> upscaleLevel;
        int lowResY = resolutionY >> upscaleLevel;

        lowResX = Math.Max(lowResX, 1);
        lowResY = Math.Max(lowResY, 1);

        // Create low resolution height map
        var lowResMap = ConstructHeightMapOriginal(lowResX, lowResY, rect, parallel);

        // Upscale to full resolution using bilinear interpolation
        return UpscaleHeightMap(lowResMap, resolutionX, resolutionY);
    }

    private float[,] ConstructHeightMapOriginal(int resolutionX, int resolutionY, Rect2I rect, bool parallel)
    {
        var heightMap = new float[resolutionX, resolutionY];
        var stepX = resolutionX > 1 ? (double)(rect.Size.X - 1) / (resolutionX - 1) : 0;
        var stepY = resolutionY > 1 ? (double)(rect.Size.Y - 1) / (resolutionY - 1) : 0;

        if (parallel)
        {
            Parallel.For(0, resolutionX, i =>
            {
                var x = resolutionX > 1 ? rect.Position.X + 0.5 + i * stepX : rect.Position.X + rect.Size.X / 2;
                for (var j = 0; j < resolutionY; j++)
                {
                    var y = resolutionY > 1 ? rect.Position.Y + 0.5 + j * stepY : rect.Position.Y + rect.Size.Y / 2;
                    heightMap[i, j] = GetHeight(x, y);
                }
            });
        }
        else
        {
            for (var i = 0; i < resolutionX; i++)
            {
                var x = resolutionX > 1 ? rect.Position.X + i * stepX : rect.Position.X + rect.Size.X / 2;
                for (var j = 0; j < resolutionY; j++)
                {
                    var y = resolutionY > 1 ? rect.Position.Y + j * stepY : rect.Position.Y + rect.Size.Y / 2;
                    heightMap[i, j] = GetHeight(x, y);
                }
            }
        }

        return heightMap;
    }

    private float[,] UpscaleHeightMap(float[,] lowResMap, int targetX, int targetY)
    {
        int lowResX = lowResMap.GetLength(0);
        int lowResY = lowResMap.GetLength(1);

        var highResMap = new float[targetX, targetY];

        // Handle case where lowRes is 1x1
        if (lowResX == 1 && lowResY == 1)
        {
            var val = lowResMap[0, 0];
            for (int x = 0; x < targetX; x++)
                for (int y = 0; y < targetY; y++)
                    highResMap[x, y] = val;
            return highResMap;
        }

        // Handle case where one dimension is 1
        if (lowResX == 1)
        {
            for (int y = 0; y < targetY; y++)
            {
                float t = (float)y / (targetY - 1) * (lowResY - 1);
                int y0 = (int)Math.Floor(t);
                int y1 = Math.Min(y0 + 1, lowResY - 1);
                float ty = t - y0;

                float val = Lerp(lowResMap[0, y0], lowResMap[0, y1], ty);
                for (int x = 0; x < targetX; x++)
                    highResMap[x, y] = val;
            }
            return highResMap;
        }

        if (lowResY == 1)
        {
            for (int x = 0; x < targetX; x++)
            {
                float t = (float)x / (targetX - 1) * (lowResX - 1);
                int x0 = (int)Math.Floor(t);
                int x1 = Math.Min(x0 + 1, lowResX - 1);
                float tx = t - x0;

                float val = Lerp(lowResMap[x0, 0], lowResMap[x1, 0], tx);
                for (int y = 0; y < targetY; y++)
                    highResMap[x, y] = val;
            }
            return highResMap;
        }

        // Bilinear interpolation for 2D case
        for (int x = 0; x < targetX; x++)
        {
            float tx = (float)x / (targetX - 1) * (lowResX - 1);
            int x0 = (int)Math.Floor(tx);
            int x1 = Math.Min(x0 + 1, lowResX - 1);
            float fx = tx - x0;

            for (int y = 0; y < targetY; y++)
            {
                float ty = (float)y / (targetY - 1) * (lowResY - 1);
                int y0 = (int)Math.Floor(ty);
                int y1 = Math.Min(y0 + 1, lowResY - 1);
                float fy = ty - y0;

                // Bilinear interpolation
                float v00 = lowResMap[x0, y0];
                float v10 = lowResMap[x1, y0];
                float v01 = lowResMap[x0, y1];
                float v11 = lowResMap[x1, y1];

                float v0 = Lerp(v00, v10, fx);
                float v1 = Lerp(v01, v11, fx);
                highResMap[x, y] = Lerp(v0, v1, fy);
            }
        }

        return highResMap;
    }

    public float[,] ConstructChunkHeightMap(Rect2I chunkRect, int upscaleLevel = 2)
    {
        // Center
        var heightMap = new float[chunkRect.Size.X, chunkRect.Size.Y];
        var centerRect = new Rect2I(chunkRect.Position + Vector2I.One, chunkRect.Size - 2 * Vector2I.One);
        var centerHeightMap = ConstructHeightMap(chunkRect.Size.X - 2, chunkRect.Size.Y - 2, centerRect, upscaleLevel: upscaleLevel);

        for (int x = 1; x < chunkRect.Size.X - 1; x++)
            for (int y = 1; y < chunkRect.Size.Y - 1; y++)
                heightMap[x, y] = centerHeightMap[x - 1, y - 1];

        // Four edges
        var topRect = new Rect2I(chunkRect.Position + new Vector2I(1, chunkRect.Size.Y - 1), chunkRect.Size.X - 2, 1);
        var topHeightMap = ConstructHeightMap(chunkRect.Size.X - 2, 1, topRect, upscaleLevel: upscaleLevel);
        var bottomRect = new Rect2I(chunkRect.Position + new Vector2I(1, 0), chunkRect.Size.X - 2, 1);
        var bottomHeightMap = ConstructHeightMap(chunkRect.Size.X - 2, 1, bottomRect, upscaleLevel: upscaleLevel);
        for (int x = 1; x < chunkRect.Size.X - 1; x++)
        {
            heightMap[x, chunkRect.Size.Y - 1] = topHeightMap[x - 1, 0];
            heightMap[x, 0] = bottomHeightMap[x - 1, 0];
        }

        var leftRect = new Rect2I(chunkRect.Position + new Vector2I(0, 1), 1, chunkRect.Size.Y - 2);
        var leftHeightMap = ConstructHeightMap(1, chunkRect.Size.Y - 2, leftRect, upscaleLevel: upscaleLevel);
        var rightRect = new Rect2I(chunkRect.Position + new Vector2I(chunkRect.Size.X - 1, 1), 1, chunkRect.Size.Y - 2);
        var rightHeightMap = ConstructHeightMap(1, chunkRect.Size.Y - 2, rightRect, upscaleLevel: upscaleLevel);
        for (int y = 1; y < chunkRect.Size.Y - 1; y++)
        {
            heightMap[0, y] = leftHeightMap[0, y - 1];
            heightMap[chunkRect.Size.X - 1, y] = rightHeightMap[0, y - 1];
        }

        // Four corners
        heightMap[0, 0] = GetHeight(chunkRect.Position.X + 0.5, chunkRect.Position.Y + 0.5);
        heightMap[chunkRect.Size.X - 1, 0] = GetHeight(chunkRect.End.X - 0.5, chunkRect.Position.Y + 0.5);
        heightMap[0, chunkRect.Size.Y - 1] = GetHeight(chunkRect.Position.X + 0.5, chunkRect.End.Y - 0.5);
        heightMap[chunkRect.Size.X - 1, chunkRect.Size.Y - 1] = GetHeight(chunkRect.End.X - 0.5, chunkRect.End.Y - 0.5);

        return heightMap;
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
