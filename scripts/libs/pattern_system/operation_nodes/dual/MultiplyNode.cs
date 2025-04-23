namespace PatternSystem;

public class MultiplyNode : DualChildOperationNode
{
    public MultiplyNode(PatternTreeNode primaryChild, PatternTreeNode secondaryChild) : base(primaryChild,
        secondaryChild)
    {
    }

    protected override double PerformOperation(double primaryValue, double secondaryValue)
    {
        return primaryValue * secondaryValue;
    }
}