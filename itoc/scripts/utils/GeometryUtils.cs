using Godot;

/// <summary>
///     Provides utility methods for geometric calculations.
/// </summary>
public static class GeometryUtils
{
    /// <summary>
    ///     Calculates the area of a triangle defined by three 2D points.
    /// </summary>
    /// <returns>The area of the triangle</returns>
    public static double CalculateTriangleArea(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        return Mathf.Abs((p0.X * (p1.Y - p2.Y) + p1.X * (p2.Y - p0.Y) + p2.X * (p0.Y - p1.Y)) / 2.0);
    }

    /// <summary>
    ///     Calculates the area of a triangle defined by three 3D points.
    /// </summary>
    /// <returns>The area of the triangle</returns>
    public static double CalculateTriangleArea(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        var u = p1 - p0;
        var v = p2 - p0;
        return u.Cross(v).Length() / 2.0;
    }

    /// <summary>
    ///     Calculates the area of a polygon defined by an array of vertices.
    ///     Uses the Shoelace formula (Gauss's area formula).
    /// </summary>
    /// <param name="vertices">Array of polygon vertices in order</param>
    /// <returns>The area of the polygon, or 0 if the polygon has fewer than 3 vertices</returns>
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

    /// <summary>
    ///     Calculates the barycentric coordinates of a point relative to a triangle.
    /// </summary>
    /// <param name="point">The point to find coordinates for</param>
    /// <param name="p0">First vertex of the triangle</param>
    /// <param name="p1">Second vertex of the triangle</param>
    /// <param name="p2">Third vertex of the triangle</param>
    /// <returns>
    ///     Vector3 containing the barycentric coordinates (u,v,w).
    ///     (1,0,0) means the point is at p0,
    ///     (0,1,0) means the point is at p1,
    ///     (0,0,1) means the point is at p2.
    /// </returns>
    public static Vector3 GetBarycentricCoordinates(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        // Compute vectors
        var v0 = p2 - p0;
        var v1 = p1 - p0;
        var v2 = point - p0;

        // Compute dot products
        var dot00 = v0.Dot(v0);
        var dot01 = v0.Dot(v1);
        var dot02 = v0.Dot(v2);
        var dot11 = v1.Dot(v1);
        var dot12 = v1.Dot(v2);

        // Compute barycentric coordinates
        var invDenom = 1.0 / (dot00 * dot11 - dot01 * dot01);
        var w = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
        var u = 1.0 - w - v;

        return new Vector3(u, v, w);
    }

    /// <summary>
    ///     Determines if a point is inside a triangle and provides the barycentric position.
    /// </summary>
    /// <param name="point">The point to check</param>
    /// <param name="barycentricPos">Output parameter containing the barycentric coordinates</param>
    /// <returns>True if the point is in the triangle, false otherwise</returns>
    public static bool IsPointInTriangle(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2, out Vector3 barycentricPos)
    {
        barycentricPos = GetBarycentricCoordinates(point, p0, p1, p2);
        return barycentricPos.X >= 0 && barycentricPos.Y >= 0 && barycentricPos.Z >= 0;
    }

    /// <summary>
    ///     Determines if a point is inside a triangle.
    /// </summary>
    /// <param name="point">The point to check</param>
    /// <returns>True if the point is in the triangle, false otherwise</returns>
    public static bool IsPointInTriangle(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        return IsPointInTriangle(point, p0, p1, p2, out _);
    }

    /// <summary>
    ///     Calculates the normal vector of a triangle in 3D space.
    /// </summary>
    /// <param name="faceUp">If true, ensures the normal points upward (Y &gt; 0)</param>
    /// <returns>The normalized normal vector of the triangle</returns>
    public static Vector3 CalculateTriangleNormal(Vector3 p0, Vector3 p1, Vector3 p2, bool faceUp = true)
    {
        var u = p1 - p0;
        var v = p2 - p0;
        var norm = u.Cross(v).Normalized();
        if (faceUp && norm.Y < 0)
            norm = -norm;

        return norm;
    }
}