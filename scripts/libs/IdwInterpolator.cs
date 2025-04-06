using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Supercluster.KDTree;

public class IdwInterpolator
{
    private readonly KDTree<float, float> _kdTree;
    private readonly double _power;
    private readonly int _numNeighbors;

    private static double L2Norm(float[] x, float[] y)
    {
        double dist = 0;
        for (int i = 0; i < x.Length; i++)
        {
            dist += (x[i] - y[i]) * (x[i] - y[i]);
        }

        return dist;
    }

    public IdwInterpolator(IEnumerable<Vector2> positions, IEnumerable<float> heights, double power = 2.0, int numNeighbors = 10)
    {
        var positionsList = positions.ToList();
        var heightsList = heights.ToList();

        if (positionsList.Count != heightsList.Count)
            throw new ArgumentException("Positions and heights must have the same number of elements.");

        var pointsData = positions.Select(p => new float[] { p.X, p.Y }).ToArray();

        _kdTree = new KDTree<float, float>(2, pointsData, heights.ToArray(), L2Norm);

        _power = power;
        _numNeighbors = numNeighbors;
    }

    public float GetHeight(float x, float y)
    {
        var neighbors = _kdTree.NearestNeighbors([x, y], _numNeighbors);

        double totalWeight = 0.0;
        double weightedSum = 0.0;

        foreach (var (pos, height) in neighbors)
        {
            var distance = L2Norm(pos, [x, y]);

            if (distance <= 0.0f)
            {
                return height;
            }

            double weight = 1.0 / Mathf.Pow(distance, _power);
            weightedSum += weight * height;
            totalWeight += weight;
        }

        if (totalWeight <= 0.0)
        {
            // This should not happen unless all weights are zero, which is impossible with distance > 0
            return 0.0f;
        }

        return (float)(weightedSum / totalWeight);
    }


    public static float[,] ConstructHeightMap(IEnumerable<Vector2> positions, IEnumerable<float> heights, int resolutionX, int resolutionY, Rect2 rect, double power = 2.0, int numNeighbors = 10)
    {
        var interpolator = new IdwInterpolator(positions, heights, power, numNeighbors);

        float[,] heightMap = new float[resolutionX, resolutionY];
        float stepX = rect.Size.X / (resolutionX - 1);
        float stepY = rect.Size.Y / (resolutionY - 1);

        Parallel.For(0, resolutionX, i =>
        {
            float x = rect.Position.X + i * stepX;
            for (int j = 0; j < resolutionY; j++)
            {
                float y = rect.Position.Y + j * stepY;
                heightMap[i, j] = interpolator.GetHeight(x, y);
            }
        });

        return heightMap;
    }
}
