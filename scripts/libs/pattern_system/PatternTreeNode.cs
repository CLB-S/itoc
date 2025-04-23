using System.Collections.Generic;

namespace PatternSystem;

public abstract class PatternTreeNode
{
    public abstract double Evaluate(double x, double y);
    public abstract double Evaluate(double x, double y, double z);
}