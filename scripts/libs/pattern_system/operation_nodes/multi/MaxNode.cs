using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public class MaxNode : MultiChildOperationNode
{
    public MaxNode(IEnumerable<PatternTreeNode> children) : base(children)
    {
    }

    protected override double PerformOperation(IEnumerable<double> values)
    {
        return values.Max();
    }
}