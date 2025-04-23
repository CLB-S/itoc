using System;
using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public abstract class MultiChildOperationNode : PatternTreeNode, IOperator
{
    private List<PatternTreeNode> _children;

    public IEnumerable<PatternTreeNode> Children => _children;

    public MultiChildOperationNode(IEnumerable<PatternTreeNode> children)
    {
        _children = children.ToList();
    }

    protected abstract double PerformOperation(IEnumerable<double> values);

    public override double Evaluate(double x, double y)
    {
        if (_children.Count == 0) return 0;

        var values = _children.Select(child => child.Evaluate(x, y));
        return PerformOperation(values);
    }

    public override double Evaluate(double x, double y, double z)
    {
        if (_children.Count == 0) return 0;

        var values = _children.Select(child => child.Evaluate(x, y, z));
        return PerformOperation(values);
    }
}