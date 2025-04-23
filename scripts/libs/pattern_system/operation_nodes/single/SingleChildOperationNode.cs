using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public abstract class SingleChildOperationNode : PatternTreeNode, IOperator
{
    private PatternTreeNode _child;

    public IEnumerable<PatternTreeNode> Children => [_child];

    public SingleChildOperationNode(PatternTreeNode child)
    {
        _child = child;
    }

    protected abstract double PerformOperation(double value);

    public override double Evaluate(double x, double y)
    {
        if (_child == null) return 0;

        var value = _child.Evaluate(x, y);
        return PerformOperation(value);
    }

    public override double Evaluate(double x, double y, double z)
    {
        if (_child == null) return 0;

        var value = _child.Evaluate(x, y, z);
        return PerformOperation(value);
    }
}