namespace PatternSystem;

public class MinusNode : DualChildOperationNode
{
    public MinusNode(PatternTreeNode primaryChild, PatternTreeNode secondaryChild) : base(primaryChild, secondaryChild)
    {
    }

    protected override double PerformOperation(double primaryValue, double secondaryValue)
    {
        return primaryValue - secondaryValue;
    }
}