using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Supercluster.KDTree;

public class IdwInterpolator
{
    private readonly KDTree<double, double> _kdTree;
    private readonly int _numNeighbors;
    private readonly double _power;

    public IdwInterpolator(IEnumerable<Vector2> positions, IEnumerable<double> heights, double power = 2.0,
        int numNeighbors = 10)
    {
        var positionsList = positions.ToList();
        var heightsList = heights.ToList();

        if (positionsList.Count != heightsList.Count)
            throw new ArgumentException("Positions and heights must have the same number of elements.");

        var pointsData = positions.Select(p => new[] { p.X, p.Y }).ToArray();

        _kdTree = new KDTree<double, double>(2, pointsData, heights.ToArray(), L2Norm);

        _power = power;
        _numNeighbors = numNeighbors;
    }

    private static double L2Norm(double[] x, double[] y)
    {
        double dist = 0;
        for (var i = 0; i < x.Length; i++) dist += (x[i] - y[i]) * (x[i] - y[i]);

        return dist;
    }

    public double GetHeight(double x, double y)
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
            return 0.0;

        return weightedSum / totalWeight;
    }


    public double[,] ConstructHeightMap(int resolutionX, int resolutionY, Rect2 rect)
    {
        if (resolutionX == 1 || resolutionY == 1)
            throw new ArgumentException("Resolution must be greater than 1."); // TODO: lazy to implement this

        var heightMap = new double[resolutionX, resolutionY];
        var stepX = rect.Size.X / (resolutionX - 1);
        var stepY = rect.Size.Y / (resolutionY - 1);

        Parallel.For(0, resolutionX, i =>
        {
            var x = rect.Position.X + i * stepX;
            for (var j = 0; j < resolutionY; j++)
            {
                var y = rect.Position.Y + j * stepY;
                heightMap[i, j] = GetHeight(x, y);
            }
        });

        return heightMap;
    }
}