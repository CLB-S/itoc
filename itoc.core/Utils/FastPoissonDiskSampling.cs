// Adapted from https://gist.github.com/a3geek/8532817159b77c727040cf67c92af322

// The algorithm is from the "Fast Poisson Disk Sampling in Arbitrary Dimensions" paper by Robert Bridson.
// https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf

using Godot;

namespace ITOC.Core.Utils;

public static class FastPoissonDiskSampling
{
    public const double InvertRootTwo = 0.70710678118; // Becaust two dimension grid.
    public const int DefaultIterationPerPoint = 30;

    public static List<Vector2> Sampling(
        Vector2 bottomLeft,
        Vector2 topRight,
        double minimumDistance,
        RandomNumberGenerator rng
    ) => Sampling(bottomLeft, topRight, minimumDistance, rng, DefaultIterationPerPoint);

    public static List<Vector2> Sampling(
        Vector2 bottomLeft,
        Vector2 topRight,
        double minimumDistance,
        RandomNumberGenerator rng,
        int iterationPerPoint
    )
    {
        var settings = GetSettings(
            bottomLeft,
            topRight,
            minimumDistance,
            iterationPerPoint <= 0 ? DefaultIterationPerPoint : iterationPerPoint
        );

        var bags = new Bags
        {
            Grid = new Vector2?[settings.GridWidth + 1, settings.GridHeight + 1],
            SamplePoints = new List<Vector2>(),
            ActivePoints = new List<Vector2>(),
        };

        GetFirstPoint(settings, bags, rng);

        do
        {
            var index = (int)(rng.Randi() % bags.ActivePoints.Count); // Random.Range(0, bags.ActivePoints.Count);

            var point = bags.ActivePoints[index];

            var found = false;
            for (var k = 0; k < settings.IterationPerPoint; k++)
                found = found | GetNextPoint(point, settings, bags, rng);

            if (found == false)
                bags.ActivePoints.RemoveAt(index);
        } while (bags.ActivePoints.Count > 0);

        return bags.SamplePoints;
    }

    #region "Structures"

    private class Settings
    {
        public Vector2 BottomLeft;

        public double CellSize;
        public Vector2 Center;
        public Rect2 Dimension;
        public int GridHeight;
        public int GridWidth;
        public int IterationPerPoint;

        public double MinimumDistance;
        public Vector2 TopRight;
    }

    private class Bags
    {
        public List<Vector2> ActivePoints;
        public Vector2?[,] Grid;
        public List<Vector2> SamplePoints;
    }

    #endregion

    #region "Algorithm Calculations"

    private static bool GetNextPoint(
        Vector2 point,
        Settings set,
        Bags bags,
        RandomNumberGenerator rng
    )
    {
        var found = false;
        var p = GetRandPosInCircle(set.MinimumDistance, 2 * set.MinimumDistance, rng) + point;

        if (set.Dimension.HasPoint(p) == false)
            return false;

        var minimum = set.MinimumDistance * set.MinimumDistance;
        var index = GetGridIndex(p, set);
        var drop = false;

        // Although it is Mathf.CeilToInt(set.MinimumDistance / set.CellSize) in the formula, It will be 2 after all.
        var around = 2;
        var fieldMin = new Vector2I(Mathf.Max(0, index.X - around), Mathf.Max(0, index.Y - around));
        var fieldMax = new Vector2I(
            Mathf.Min(set.GridWidth, index.X + around),
            Mathf.Min(set.GridHeight, index.Y + around)
        );

        for (var i = fieldMin.X; i <= fieldMax.X && drop == false; i++)
        for (var j = fieldMin.Y; j <= fieldMax.Y && drop == false; j++)
        {
            var q = bags.Grid[i, j];
            if (q.HasValue && (q.Value - p).LengthSquared() <= minimum)
                drop = true;
        }

        if (drop == false)
        {
            found = true;

            bags.SamplePoints.Add(p);
            bags.ActivePoints.Add(p);
            bags.Grid[index.X, index.Y] = p;
        }

        return found;
    }

    private static void GetFirstPoint(Settings set, Bags bags, RandomNumberGenerator rng)
    {
        var first = new Vector2(
            rng.RandfRange(set.BottomLeft.X, set.TopRight.X),
            rng.RandfRange(set.BottomLeft.Y, set.TopRight.Y)
        );

        var index = GetGridIndex(first, set);

        bags.Grid[index.X, index.Y] = first;
        bags.SamplePoints.Add(first);
        bags.ActivePoints.Add(first);
    }

    #endregion

    #region "Utils"

    private static Vector2I GetGridIndex(Vector2 point, Settings set) =>
        new Vector2I(
            Mathf.FloorToInt((point.X - set.BottomLeft.X) / set.CellSize),
            Mathf.FloorToInt((point.Y - set.BottomLeft.Y) / set.CellSize)
        );

    private static Settings GetSettings(Vector2 bl, Vector2 tr, double min, int iteration)
    {
        var dimension = tr - bl;
        var cell = min * InvertRootTwo;

        return new Settings
        {
            BottomLeft = bl,
            TopRight = tr,
            Center = (bl + tr) * 0.5,
            Dimension = new Rect2(bl, dimension),

            MinimumDistance = min,
            IterationPerPoint = iteration,

            CellSize = cell,
            GridWidth = Mathf.CeilToInt(dimension.X / cell),
            GridHeight = Mathf.CeilToInt(dimension.Y / cell),
        };
    }

    private static Vector2 GetRandPosInCircle(
        double fieldMin,
        double fieldMax,
        RandomNumberGenerator rng
    )
    {
        var theta = rng.RandfRange(0, Mathf.Pi * 2);
        var radius = Mathf.Sqrt(rng.RandfRange(fieldMin * fieldMin, fieldMax * fieldMax));

        return new Vector2(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta));
    }

    #endregion
}
