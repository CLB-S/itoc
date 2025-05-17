using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public enum MultiOperationType
{
    Add,
    Multiply,
    Min,
    Max,
    Average
}

public class MultiChildOperationNode : PatternTreeNode, IOperator
{
    private readonly List<PatternTreeNode> _children;

    public IEnumerable<PatternTreeNode> Children => _children;
    public MultiOperationType OperationType { get; protected set; }

    public MultiChildOperationNode(IEnumerable<PatternTreeNode> children,
        MultiOperationType operationType = MultiOperationType.Add)
    {
        _children = children.ToList();
        OperationType = operationType;
    }

    protected virtual double PerformOperation(IEnumerable<double> values)
    {
        return OperationType switch
        {
            MultiOperationType.Multiply => values.Aggregate(1.0, (acc, value) => acc * value),
            MultiOperationType.Min => values.Min(),
            MultiOperationType.Max => values.Max(),
            MultiOperationType.Average => values.Average(),
            _ => values.Sum()
        };
    }

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