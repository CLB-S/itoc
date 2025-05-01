using Godot;
using System;

public static class LinearInterpolator
{
    public static double Interpolate(Vector2 p0, Vector2 p1, Vector2 p2,
        double h0, double h1, double h2, Vector2 target)
    {
        // Compute vectors for barycentric coordinates
        Vector2 v0 = p1 - p0;
        Vector2 v1 = p2 - p0;
        Vector2 v2 = target - p0;

        double d00 = v0.Dot(v0);
        double d01 = v0.Dot(v1);
        double d11 = v1.Dot(v1);
        double d20 = v2.Dot(v0);
        double d21 = v2.Dot(v1);

        double denom = d00 * d11 - d01 * d01;

        // Handle degenerate triangles (unlikely in valid input)
        if (Mathf.Abs(denom) < 1e-6)
        {
            // Fallback to average height if the triangle is degenerate
            return (h0 + h1 + h2) / 3.0;
        }

        double v = (d11 * d20 - d01 * d21) / denom;
        double w = (d00 * d21 - d01 * d20) / denom;
        double u = 1.0 - v - w;

        return u * h0 + v * h1 + w * h2;
    }
}