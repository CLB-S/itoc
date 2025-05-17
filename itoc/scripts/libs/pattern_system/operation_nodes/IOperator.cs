using System.Collections.Generic;

namespace PatternSystem;

public interface IOperator
{
    IEnumerable<PatternTreeNode> Children { get; }
}