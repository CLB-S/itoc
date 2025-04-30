using Godot;

public static class GeometryUtils
{
    public static double CalculatePolygonArea(Vector2[] vertices)
    {
        if (vertices == null || vertices.Length < 3)
            return 0;

        double area = 0;
        var n = vertices.Length;

        for (var i = 0; i < n; i++)
        {
            var current = vertices[i];
            var next = vertices[(i + 1) % n];
            area += current.X * next.Y - current.Y * next.X;
        }

        return Mathf.Abs(area) / 2f;
    }

    public static bool IsPointInTriangle(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        // Compute vectors
        Vector2 v0 = p2 - p0;
        Vector2 v1 = p1 - p0;
        Vector2 v2 = point - p0;

        // Compute dot products
        double dot00 = v0.Dot(v0);
        double dot01 = v0.Dot(v1);
        double dot02 = v0.Dot(v2);
        double dot11 = v1.Dot(v1);
        double dot12 = v1.Dot(v2);

        // Compute barycentric coordinates
        double invDenom = 1.0 / (dot00 * dot11 - dot01 * dot01);
        double u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        double v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is in triangle
        return (u >= 0) && (v >= 0) && (u + v < 1);
    }
}