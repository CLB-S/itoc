using System;

namespace PatternSystem;

public class AbsNode : SingleChildOperationNode
{
    public AbsNode(PatternTreeNode child) : base(child)
    {
    }

    protected override double PerformOperation(double value)
    {
        return Math.Abs(value);
    }
}