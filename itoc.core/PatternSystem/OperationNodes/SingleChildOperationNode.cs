namespace ITOC.Core.PatternSystem;

public enum SingleOperationType
{
    Abs,
    Negate,
    Sqrt,
    Sin,
    Cos,
    Tan,
    Asin,
    Acos,
    Atan,
    Log,
    Exp,
    Floor,
    Ceil,
    Round,
    Sign,
    Clamp,
    Inverse,
    Square,
    SquareRoot,
    Cube,
    CubeRoot,
    Log10,
}

public class SingleChildOperationNode : PatternTreeNode, IOperator
{
    private readonly PatternTreeNode _child;

    public IEnumerable<PatternTreeNode> Children => [_child];
    public SingleOperationType OperationType { get; protected set; }

    public SingleChildOperationNode(PatternTreeNode child, SingleOperationType operationType)
    {
        _child = child;
        OperationType = operationType;
    }

    protected virtual double PerformOperation(double value) =>
        OperationType switch
        {
            SingleOperationType.Abs => Math.Abs(value),
            SingleOperationType.Negate => -value,
            SingleOperationType.Sqrt => Math.Sqrt(value),
            SingleOperationType.Sin => Math.Sin(value),
            SingleOperationType.Cos => Math.Cos(value),
            SingleOperationType.Tan => Math.Tan(value),
            SingleOperationType.Asin => Math.Asin(value),
            SingleOperationType.Acos => Math.Acos(value),
            SingleOperationType.Atan => Math.Atan(value),
            SingleOperationType.Log => Math.Log(value),
            SingleOperationType.Exp => Math.Exp(value),
            SingleOperationType.Floor => Math.Floor(value),
            SingleOperationType.Ceil => Math.Ceiling(value),
            SingleOperationType.Round => Math.Round(value),
            SingleOperationType.Sign => Math.Sign(value),
            SingleOperationType.Clamp => Math.Clamp(value, 0, 1),
            SingleOperationType.Inverse => 1 / value,
            SingleOperationType.Square => value * value,
            SingleOperationType.SquareRoot => Math.Sqrt(value),
            SingleOperationType.Cube => value * value * value,
            SingleOperationType.CubeRoot => Math.Pow(value, 1.0 / 3.0),
            _ => value,
        };

    public override double Evaluate(double x, double y)
    {
        if (_child == null)
            return 0;

        var value = _child.Evaluate(x, y);
        return PerformOperation(value);
    }

    public override double Evaluate(double x, double y, double z)
    {
        if (_child == null)
            return 0;

        var value = _child.Evaluate(x, y, z);
        return PerformOperation(value);
    }
}
