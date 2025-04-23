using System;
using System.Collections.Generic;
using System.Linq;

namespace PatternSystem;

public interface IOperator
{
    IEnumerable<PatternTreeNode> Children { get; }
}