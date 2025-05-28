using System.Collections.Generic;

namespace ITOC.Core.PatternSystem;

public interface IOperator
{
    IEnumerable<PatternTreeNode> Children { get; }
}