using System;
using System.Collections.Generic;
using Godot;

namespace PatternSystem;

public enum DualOperationType
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Mod,
    Power,
    Min,
    Max
}

public class DualChildOperationNode : PatternTreeNode, IOperator
{
    private readonly PatternTreeNode _primaryChild;
    private readonly PatternTreeNode _secondaryChild;

    public IEnumerable<PatternTreeNode> Children => [_primaryChild, _secondaryChild];
    public DualOperationType OperationType { get; protected set; }

    public DualChildOperationNode(PatternTreeNode primaryChild, PatternTreeNode secondaryChild,
        DualOperationType operationType = DualOperationType.Add)
    {
        _primaryChild = primaryChild;
        _secondaryChild = secondaryChild;
        OperationType = operationType;
    }

    protected virtual double PerformOperation(double primaryValue, double secondaryValue)
    {
        return OperationType switch
        {
            DualOperationType.Subtract => primaryValue - secondaryValue,
            DualOperationType.Multiply => primaryValue * secondaryValue,
            DualOperationType.Divide => primaryValue / secondaryValue,
            DualOperationType.Mod => primaryValue % secondaryValue,
            DualOperationType.Power => Math.Pow(primaryValue, secondaryValue),
            DualOperationType.Min => Math.Min(primaryValue, secondaryValue),
            DualOperationType.Max => Math.Max(primaryValue, secondaryValue),
            _ => primaryValue + secondaryValue,
        };
    }

    public override double Evaluate(double x, double y)
    {
        if (_primaryChild == null || _secondaryChild == null) return 0;

        var primaryValue = _primaryChild.Evaluate(x, y);
        var secondaryValue = _secondaryChild.Evaluate(x, y);
        return PerformOperation(primaryValue, secondaryValue);
    }

    public override double Evaluate(double x, double y, double z)
    {
        if (_primaryChild == null || _secondaryChild == null) return 0;

        var primaryValue = _primaryChild.Evaluate(x, y, z);
        var secondaryValue = _secondaryChild.Evaluate(x, y, z);
        return PerformOperation(primaryValue, secondaryValue);
    }
}