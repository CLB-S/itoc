namespace ITOC.Core.PatternSystem;

public class PatternTree : PatternTreeNode
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public PatternTreeNode RootNode { get; }

    public PatternTree(string patternId, string patternName, PatternTreeNode root)
    {
        // TODO: Use identifier.

        Id = patternId;
        Name = patternName;

        RootNode = root;
    }

    public override double Evaluate(double x, double y)
    {
        return RootNode.Evaluate(x, y);
    }

    public override double Evaluate(double x, double y, double z)
    {
        return RootNode.Evaluate(x, y, z);
    }
}