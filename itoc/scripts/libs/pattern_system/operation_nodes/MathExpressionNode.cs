using System.Collections.Generic;
using System.Linq;
using MathEvaluation.Context;
using MathEvaluation.Extensions;

namespace PatternSystem;

public class MathExpressionNode : PatternTreeNode, IOperator
{
    private static MathContext _mathContext { get; } = new ScientificMathContext();

    private readonly List<PatternTreeNode> _children;
    public IEnumerable<PatternTreeNode> Children => _children;

    public string MathExpression { get; }


    /// <summary>
    ///     Creates a new instance of the MathNode class with given math expression.
    ///     See
    ///     <see
    ///         href="https://github.com/AntonovAnton/math.evaluation?tab=readme-ov-file#scientific-math-context-using-scientificmathcontext-class">
    ///         AntonovAnton/math.evaluation
    ///     </see>
    ///     for supported math operations.
    /// </summary>
    /// <param name="mathExpression">
    ///     <c>x</c> or <c>x1</c> for the child.
    ///     <c>Px</c>, <c>Py</c>, <c>Pz</c> for the coordinates.
    /// </param>
    public MathExpressionNode(string mathExpression)
    {
        _children = [];
        MathExpression = mathExpression;
    }

    /// <summary>
    ///     Creates a new instance of the MathNode class with given child and math expression.
    ///     See
    ///     <see
    ///         href="https://github.com/AntonovAnton/math.evaluation?tab=readme-ov-file#scientific-math-context-using-scientificmathcontext-class">
    ///         AntonovAnton/math.evaluation
    ///     </see>
    ///     for supported math operations.
    /// </summary>
    /// <param name="mathExpression">
    ///     <c>x</c> or <c>x1</c> for the child.
    ///     <c>Px</c>, <c>Py</c>, <c>Pz</c> for the coordinates.
    /// </param>
    public MathExpressionNode(PatternTreeNode child, string mathExpression)
    {
        _children = [child];
        MathExpression = mathExpression;
    }

    /// <summary>
    ///     Creates a new instance of the MathNode class with given children and math expression.
    ///     See
    ///     <see
    ///         href="https://github.com/AntonovAnton/math.evaluation?tab=readme-ov-file#scientific-math-context-using-scientificmathcontext-class">
    ///         AntonovAnton/math.evaluation
    ///     </see>
    ///     for supported math operations.
    /// </summary>
    /// <param name="mathExpression">
    ///     x<c>n</c> for <c>n</c>-th child.
    ///     Eg. <c>x</c> or <c>x1</c> for first child, <c>x2</c> for second child.
    ///     <c>Px</c>, <c>Py</c>, <c>Pz</c> for the coordinates.
    /// </param>
    public MathExpressionNode(IEnumerable<PatternTreeNode> children, string mathExpression)
    {
        _children = children.ToList();
        MathExpression = mathExpression;
    }

    protected double PerformOperation(IEnumerable<double> values, Dictionary<string, double> paramDict)
    {
        var valuesArray = values.ToArray();

        paramDict["x"] = valuesArray[0];
        for (var i = 0; i < valuesArray.Length; i++)
            paramDict[$"x{i}"] = valuesArray[i];

        return MathExpression.Evaluate(paramDict, _mathContext);
    }

    public override double Evaluate(double x, double y)
    {
        if (_children.Count == 0) return MathExpression.Evaluate(new { Px = x, Py = y }, _mathContext);

        var values = _children.Select(child => child.Evaluate(x, y));
        var dict = new Dictionary<string, double>(_children.Count + 1 + 2)
        {
            ["Px"] = x,
            ["Py"] = y
        };

        return PerformOperation(values, dict);
    }

    public override double Evaluate(double x, double y, double z)
    {
        if (_children.Count == 0) return MathExpression.Evaluate(new { Px = x, Py = y, Pz = z }, _mathContext);

        var values = _children.Select(child => child.Evaluate(x, y, z));
        var dict = new Dictionary<string, double>(_children.Count + 1 + 3)
        {
            ["Px"] = x,
            ["Py"] = y,
            ["Pz"] = z
        };

        return PerformOperation(values, dict);
    }
}