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
}