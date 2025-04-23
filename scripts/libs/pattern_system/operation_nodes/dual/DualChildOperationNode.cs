using System;
using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public abstract class DualChildOperationNode : PatternTreeNode, IOperator
{
    private PatternTreeNode _primaryChild;
    private PatternTreeNode _secondaryChild;

    public IEnumerable<PatternTreeNode> Children => [_primaryChild, _secondaryChild];

    public DualChildOperationNode(PatternTreeNode primaryChild, PatternTreeNode secondaryChild)
    {
        _primaryChild = primaryChild;
        _secondaryChild = secondaryChild;
    }

    protected abstract double PerformOperation(double primaryValue, double secondaryValue);

    public override double Evaluate(double x, double y)
    {
        if (_primaryChild == null || _secondaryChild == null) return 0;

        var primaryValue = _primaryChild.Evaluate(x, y);
        var secondaryValue = _secondaryChild.Evaluate(x, y);
        return PerformOperation(primaryValue, secondaryValue);
    }

    public override double Evaluate(double x, double y, double z)
    {
        if (_primaryChild == null || _secondaryChild == null) return 0;

        var primaryValue = _primaryChild.Evaluate(x, y, z);
        var secondaryValue = _secondaryChild.Evaluate(x, y, z);
        return PerformOperation(primaryValue, secondaryValue);
    }
}