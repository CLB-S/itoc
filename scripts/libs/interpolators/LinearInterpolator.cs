using Godot;
using System;

public static class LinearInterpolator
{
    public static double Interpolate(Vector2 p0, Vector2 p1, Vector2 p2,
        double h0, double h1, double h2, Vector2 target)
    {
        Vector3 barycentric = GeometryUtils.GetBarycentricCoordinates(target, p0, p1, p2);
        double w = barycentric.X;
        double v = barycentric.Y;
        double u = barycentric.Z;

        // Handle degenerate triangles (unlikely in valid input)
        if (double.IsNaN(u) || double.IsNaN(v) || double.IsNaN(w))
            return (h0 + h1 + h2) / 3.0;

        return u * h0 + v * h1 + w * h2;
    }
}