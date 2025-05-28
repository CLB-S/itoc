using Godot;
using ITOC.Core.Utils;

namespace ITOC.Core.Interpolators;

public static class PhongTessellationInterpolator
{
    public static double Interpolate(Vector2 p0, Vector2 p1, Vector2 p2,
        double h0, double h1, double h2,
        Vector3 n0, Vector3 n1, Vector3 n2,
        Vector2 target, double alpha = 0.1)
    {
        // Get barycentric coordinates using the utility method
        var barycentric = GeometryUtils.GetBarycentricCoordinates(target, p0, p1, p2);
        var u = barycentric.X;
        var v = barycentric.Y;
        var w = barycentric.Z;

        // Handle degenerate triangles (unlikely in valid input)
        if (double.IsNaN(u) || double.IsNaN(v) || double.IsNaN(w))
            return (h0 + h1 + h2) / 3.0;

        // Compute heights based on each vertex's normal plane
        var H0 = h0 - (n0.X * (target.X - p0.X) + n0.Y * (target.Y - p0.Y)) / n0.Z;
        var H1 = h1 - (n1.X * (target.X - p1.X) + n1.Y * (target.Y - p1.Y)) / n1.Z;
        var H2 = h2 - (n2.X * (target.X - p2.X) + n2.Y * (target.Y - p2.Y)) / n2.Z;

        // Linear and Phong interpolated heights
        var linear = u * h0 + v * h1 + w * h2;
        var phong = u * H0 + v * H1 + w * H2;

        // Blend with alpha
        return (1.0 - alpha) * linear + alpha * phong;
    }
}