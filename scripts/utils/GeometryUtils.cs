using Godot;

public static class GeometryUtils
{
    public static double CalculateTriangleArea(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        return Mathf.Abs((p0.X * (p1.Y - p2.Y) + p1.X * (p2.Y - p0.Y) + p2.X * (p0.Y - p1.Y)) / 2.0);
    }

    public static double CalculateTriangleArea(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 u = p1 - p0;
        Vector3 v = p2 - p0;
        return u.Cross(v).Length() / 2.0;
    }

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

    public static Vector3 GetBarycentricCoordinates(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2)
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
        double w = 1.0 - u - v;

        return new Vector3(u, v, w);
    }

    public static bool IsPointInTriangle(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2, out Vector3 barycentricPos)
    {
        barycentricPos = GetBarycentricCoordinates(point, p0, p1, p2);
        return barycentricPos.X >= 0 && barycentricPos.Y >= 0 && barycentricPos.X + barycentricPos.Y <= 1;
    }

    public static bool IsPointInTriangle(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        return IsPointInTriangle(point, p0, p1, p2, out _);
    }

    public static Vector3 CalculateTriangleNormal(Vector3 p0, Vector3 p1, Vector3 p2, bool faceUp = true)
    {
        Vector3 u = p1 - p0;
        Vector3 v = p2 - p0;
        var norm = u.Cross(v).Normalized();
        if (faceUp && norm.Y < 0)
            norm = -norm;

        return norm;
    }
}