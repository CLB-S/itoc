using Godot;

namespace ITOC.Core.PatternSystem;

public abstract class PatternTreeNode
{
    public abstract double Evaluate(double x, double y);
    public abstract double Evaluate(double x, double y, double z);

    public double Evaluate(Vector2 position) => Evaluate(position.X, position.Y);
    public double Evaluate(Vector3 position) => Evaluate(position.X, position.Y, position.Z);

    public virtual void SetSeed(int seed)
    {
        if (this is IOperator operatorNode)
            foreach (var child in operatorNode.Children)
                child.SetSeed(seed++);
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

    public string ToJson()
    {
        return PatternTreeJsonConverter.Serialize(this);
    }

    public PatternTreeNode Multiply(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Multiply);
    public PatternTreeNode Add(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Add);
    public PatternTreeNode Subtract(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Subtract);
    public PatternTreeNode Divide(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Divide);
    public PatternTreeNode Mod(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Mod);
    public PatternTreeNode Power(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Power);
    public PatternTreeNode Min(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Min);
    public PatternTreeNode Max(double value) => new DualChildOperationNode(this, new ConstantNode(value), DualOperationType.Max);
    public PatternTreeNode Add(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Add);
    public PatternTreeNode Subtract(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Subtract);
    public PatternTreeNode Multiply(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Multiply);
    public PatternTreeNode Divide(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Divide);
    public PatternTreeNode Mod(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Mod);
    public PatternTreeNode Power(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Power);
    public PatternTreeNode Min(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Min);
    public PatternTreeNode Max(PatternTreeNode node) => new DualChildOperationNode(this, node, DualOperationType.Max);
}