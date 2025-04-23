namespace PatternSystem;

public class DivideNode : DualChildOperationNode
{
    public DivideNode(PatternTreeNode primaryChild, PatternTreeNode secondaryChild) : base(primaryChild, secondaryChild) { }

    protected override double PerformOperation(double primaryValue, double secondaryValue)
    {
        return primaryValue / secondaryValue;
    }
}