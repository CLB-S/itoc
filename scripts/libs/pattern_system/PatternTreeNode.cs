using Godot;

namespace PatternSystem;

public abstract class PatternTreeNode
{
    public abstract double Evaluate(double x, double y);
    public abstract double Evaluate(double x, double y, double z);

    public double Evaluate(Vector2 position)
    {
        return Evaluate(position.X, position.Y);
    }

    public double Evaluate(Vector3 position)
    {
        return Evaluate(position.X, position.Y, position.Z);
    }

    public double EvaluateSeamlessX(double x, double y, Rect2 bounds)
    {
        var mappedX = 2 * Mathf.Pi * x / bounds.Size.X;
        return Evaluate(Mathf.Cos(mappedX) * bounds.Size.X * 0.5 / Mathf.Pi,
            Mathf.Sin(mappedX) * bounds.Size.X * 0.5 / Mathf.Pi, y);
    }

    public double EvaluateSeamlessX(Vector2 position, Rect2 bounds)
    {
        return EvaluateSeamlessX(position.X, position.Y, bounds);
    }

    /// <summary>
    /// Warning: The output is not uniform.
    /// </summary>
    public double EvaluateSeamless(double x, double y, Rect2 bounds)
    {
        var mappedX = 2 * Mathf.Pi * x / bounds.Size.X;
        var mappedY = 2 * Mathf.Pi * y / bounds.Size.Y;

        return Evaluate(
            Mathf.Cos(mappedX) * bounds.Size.X * 0.5 / Mathf.Pi,
            Mathf.Sin(mappedY) * bounds.Size.Y * 0.5 / Mathf.Pi,
            Mathf.Sin(mappedX) * bounds.Size.X * 0.5 / Mathf.Pi + Mathf.Cos(mappedY) * bounds.Size.Y * 0.5 / Mathf.Pi
        );
    }

    /// <summary>
    /// Warning: The output is not uniform.
    /// </summary>
    public double EvaluateSeamless(Vector2 position, Rect2 bounds)
    {
        return EvaluateSeamless(position.X, position.Y, bounds);
    }
}