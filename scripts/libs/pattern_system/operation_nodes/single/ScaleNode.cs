namespace PatternSystem;

public class ScaleNode : SingleChildOperationNode
{
    public double Scale { get; }

    public ScaleNode(PatternTreeNode child, double scale) : base(child)
    {
        Scale = scale;
    }

    protected override double PerformOperation(double value)
    {
        return value * Scale;
    }
}