using Godot;
using Supercluster.KDTree;

namespace ITOC.Core.Interpolators;

public class IdwInterpolator
{
    private readonly KDTree<double, double> _kdTree;
    private readonly int _numNeighbors;
    private readonly double _power;

    public IdwInterpolator(
        IEnumerable<Vector2> positions,
        IEnumerable<double> heights,
        double power = 2,
        int numNeighbors = 20
    )
    {
        var positionsList = positions.ToList();
        var heightsList = heights.ToList();

        if (positionsList.Count != heightsList.Count)
            throw new ArgumentException(
                "Positions and heights must have the same number of elements."
            );

        var pointsData = positions.Select(p => new[] { p.X, p.Y }).ToArray();

        _kdTree = new KDTree<double, double>(2, pointsData, heights.ToArray(), L2Norm);

        _power = power;
        _numNeighbors = numNeighbors;
    }

    private static double L2Norm(double[] x, double[] y)
    {
        double dist = 0;
        for (var i = 0; i < x.Length; i++)
            dist += (x[i] - y[i]) * (x[i] - y[i]);

        return dist;
    }

    public double GetHeight(double x, double y)
    {
        var neighbors = _kdTree.NearestNeighbors([x, y], _numNeighbors);

        var totalWeight = 0.0;
        var weightedSum = 0.0;

        foreach (var (pos, height) in neighbors)
        {
            var distanceSqr = L2Norm(pos, [x, y]);

            // if (distance <= 0) return height;

            var weight = 1.0 / Mathf.Pow(distanceSqr + 5, _power / 2.0);
            weightedSum += weight * height;
            totalWeight += weight;
        }

        if (totalWeight <= 0.0)
            // This should not happen unless all weights are zero, which is impossible with distance > 0
            return 0.0f;

        return weightedSum / totalWeight;
    }
}
