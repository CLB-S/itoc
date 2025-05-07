using Godot;

public static class LinearInterpolator
{
    public static double Interpolate(Vector2 p0, Vector2 p1, Vector2 p2,
        double h0, double h1, double h2, Vector2 target)
    {
        var barycentric = GeometryUtils.GetBarycentricCoordinates(target, p0, p1, p2);
        var u = barycentric.X;
        var v = barycentric.Y;
        var w = barycentric.Z;

        // Handle degenerate triangles (unlikely in valid input)
        if (double.IsNaN(u) || double.IsNaN(v) || double.IsNaN(w))
            return (h0 + h1 + h2) / 3.0;

        return u * h0 + v * h1 + w * h2;
    }
}