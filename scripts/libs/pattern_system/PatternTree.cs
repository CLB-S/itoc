using System;

namespace PatternSystem;

public class PatternTree : PatternTreeNode
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public PatternTreeNode RootNode { get; }

    public PatternTree(string patternId, string patternName, PatternTreeNode root)
    {
        if (!StringUtils.IsValidId(patternId))
            throw new ArgumentException(
                $"Invalid block ID format: {patternId}. Must be in format 'pattern_id' using lowercase letters, numbers and underscores");

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