namespace ITOC.Core.PatternSystem;

public class ConstantNode : PatternTreeNode
{
    public double Value { get; }

    public ConstantNode(double value) => Value = value;

    public override double Evaluate(double x, double y) => Value;

    public override double Evaluate(double x, double y, double z) => Value;
}
