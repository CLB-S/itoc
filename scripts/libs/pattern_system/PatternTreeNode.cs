using Godot;

namespace PatternSystem;

public abstract class PatternTreeNode
{
    public double Evaluate(Vector2 point)
    {
        return Evaluate(point.X, point.Y);
    }

    public double Evaluate(Vector3 point)
    {
        return Evaluate(point.X, point.Y, point.Z);
    }

    public abstract double Evaluate(double x, double y);
    public abstract double Evaluate(double x, double y, double z);
}