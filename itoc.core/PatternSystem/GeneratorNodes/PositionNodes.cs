namespace ITOC.Core.PatternSystem;

public class PositionXNode : PatternTreeNode
{
    public override double Evaluate(double x, double y)
    {
        return x;
    }

    public override double Evaluate(double x, double y, double z)
    {
        return x;
    }
}

public class PositionYNode : PatternTreeNode
{
    public override double Evaluate(double x, double y)
    {
        return y;
    }

    public override double Evaluate(double x, double y, double z)
    {
        return y;
    }
}

public class PositionZNode : PatternTreeNode
{
    public override double Evaluate(double x, double y)
    {
        return 0;
    }

    public override double Evaluate(double x, double y, double z)
    {
        return z;
    }
}