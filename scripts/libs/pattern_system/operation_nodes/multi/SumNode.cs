using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public class SumNode : MultiChildOperationNode
{
    public SumNode(IEnumerable<PatternTreeNode> children) : base(children)
    {
    }

    protected override double PerformOperation(IEnumerable<double> values)
    {
        return values.Sum();
    }
}