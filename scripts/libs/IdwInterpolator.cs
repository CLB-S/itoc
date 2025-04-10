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

    public IdwInterpolator(IEnumerable<Vector2> positions, IEnumerable<float> heights, double power = 2.0,
        int numNeighbors = 10)
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
            var distance = L2Norm(pos, [x, y]);

            if (distance <= 0) return height;

            var weight = 1.0 / Mathf.Pow(distance, _power);
            weightedSum += weight * height;
            totalWeight += weight;
        }

        if (totalWeight <= 0.0)
            // This should not happen unless all weights are zero, which is impossible with distance > 0
            return 0.0f;

        return (float)(weightedSum / totalWeight);
    }

    public float[,] ConstructHeightMap(int resolutionX, int resolutionY, Rect2 rect, bool parallel = false, int upscaleLevel = 2)
    {
        if (resolutionX <= 1 || resolutionY <= 1)
            throw new ArgumentException("Resolution must be greater than 1.");

        // If upscaleLevel is 0 or would result in a grid smaller than 2x2, use original method
        if (upscaleLevel <= 0 || resolutionX >> upscaleLevel < 2 || resolutionY >> upscaleLevel < 2)
        {
            return ConstructHeightMapOriginal(resolutionX, resolutionY, rect, parallel);
        }

        // Calculate low resolution dimensions
        int lowResX = resolutionX >> upscaleLevel;
        int lowResY = resolutionY >> upscaleLevel;

        // Ensure we have at least 2 points in each dimension
        lowResX = Math.Max(lowResX, 2);
        lowResY = Math.Max(lowResY, 2);

        // Create low resolution height map
        var lowResMap = ConstructHeightMapOriginal(lowResX, lowResY, rect, parallel);

        // Upscale to full resolution using bilinear interpolation
        return UpscaleHeightMap(lowResMap, resolutionX, resolutionY);
    }

    private float[,] ConstructHeightMapOriginal(int resolutionX, int resolutionY, Rect2 rect, bool parallel)
    {
        var heightMap = new float[resolutionX, resolutionY];
        var stepX = rect.Size.X / (resolutionX - 1);
        var stepY = rect.Size.Y / (resolutionY - 1);

        if (parallel)
        {
            Parallel.For(0, resolutionX, i =>
            {
                var x = rect.Position.X + i * stepX;
                for (var j = 0; j < resolutionY; j++)
                {
                    var y = rect.Position.Y + j * stepY;
                    heightMap[i, j] = GetHeight(x, y);
                }
            });
        }
        else
        {
            for (var i = 0; i < resolutionX; i++)
            {
                var x = rect.Position.X + i * stepX;
                for (var j = 0; j < resolutionY; j++)
                {
                    var y = rect.Position.Y + j * stepY;
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

        // Precompute scaling factors
        float xScale = (float)(lowResX - 1) / (targetX - 1);
        float yScale = (float)(lowResY - 1) / (targetY - 1);

        for (int i = 0; i < targetX; i++)
        {
            float lowResI = i * xScale;
            int i1 = (int)lowResI;
            int i2 = Math.Min(i1 + 1, lowResX - 1);
            float xRatio = lowResI - i1;

            for (int j = 0; j < targetY; j++)
            {
                float lowResJ = j * yScale;
                int j1 = (int)lowResJ;
                int j2 = Math.Min(j1 + 1, lowResY - 1);
                float yRatio = lowResJ - j1;

                // Bilinear interpolation
                float top = Lerp(lowResMap[i1, j1], lowResMap[i2, j1], xRatio);
                float bottom = Lerp(lowResMap[i1, j2], lowResMap[i2, j2], xRatio);
                highResMap[i, j] = Lerp(top, bottom, yRatio);
            }
        }

        return highResMap;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}