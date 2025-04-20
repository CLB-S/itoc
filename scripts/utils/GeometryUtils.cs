using Godot;
using System;

public static class GeometryUtils
{
    public static double CalculatePolygonArea(Vector2[] vertices)
    {
        if (vertices == null || vertices.Length < 3)
            return 0;

        double area = 0;
        int n = vertices.Length;

        for (int i = 0; i < n; i++)
        {
            Vector2 current = vertices[i];
            Vector2 next = vertices[(i + 1) % n];
            area += current.X * next.Y - current.Y * next.X;
        }

        return Mathf.Abs(area) / 2f;
    }
}