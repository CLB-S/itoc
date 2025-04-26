using System.Collections.Generic;

namespace PatternSystem;

public class PositionTransformNode : PatternTreeNode, IOperator
{
    private readonly PatternTreeNode _targetChild;
    private readonly PatternTreeNode _xChild;
    private readonly PatternTreeNode _yChild;
    private readonly PatternTreeNode _zChild;

    public IEnumerable<PatternTreeNode> Children => [_targetChild, _xChild, _yChild, _zChild];

    public PositionTransformNode(PatternTreeNode targetNode, PatternTreeNode x = null, PatternTreeNode y = null, PatternTreeNode z = null)
    {
        _targetChild = targetNode;
        _xChild = x;
        _yChild = y;
        _zChild = z;
    }

    public override double Evaluate(double x, double y)
    {
        var xValue = _xChild?.Evaluate(x, y) ?? x;
        var yValue = _yChild?.Evaluate(x, y) ?? y;

        return _targetChild.Evaluate(xValue, yValue);
    }

    public override double Evaluate(double x, double y, double z)
    {
        var xValue = _xChild?.Evaluate(x, y, z) ?? x;
        var yValue = _yChild?.Evaluate(x, y, z) ?? y;
        var zValue = _zChild?.Evaluate(x, y, z) ?? z;

        return _targetChild.Evaluate(xValue, yValue, zValue);
    }
}