namespace ITOC.Core.PatternSystem;

public class PositionXNode : PatternTreeNode
{
    public override double Evaluate(double x, double y) => x;

    public override double Evaluate(double x, double y, double z) => x;
}

public class PositionYNode : PatternTreeNode
{
    public override double Evaluate(double x, double y) => y;

    public override double Evaluate(double x, double y, double z) => y;
}

public class PositionZNode : PatternTreeNode
{
    public override double Evaluate(double x, double y) => 0;

    public override double Evaluate(double x, double y, double z) => z;
}
