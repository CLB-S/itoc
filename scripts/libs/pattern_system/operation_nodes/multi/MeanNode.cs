using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public class MeanNode : MultiChildOperationNode
{
    public MeanNode(IEnumerable<PatternTreeNode> children) : base(children)
    {
    }

    protected override double PerformOperation(IEnumerable<double> values)
    {
        return values.Average();
    }
}