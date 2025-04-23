using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public class MinNode : MultiChildOperationNode
{
    public MinNode(IEnumerable<PatternTreeNode> children) : base(children)
    {
    }

    protected override double PerformOperation(IEnumerable<double> values)
    {
        return values.Min();
    }
}