using System;
using System.Collections.Generic;
using Godot;

namespace PatternSystem;

public class PatternTreeBuilder
{
    private PatternTreeNode _currentNode;
    private readonly string _id;
    private readonly string _name;

    public PatternTreeBuilder()
    {
    }

    public PatternTreeBuilder(string id, string name)
    {
        _id = id;
        _name = name;
    }

    public PatternTreeBuilder WithNode(PatternTreeNode node)
    {
        _currentNode = node;
        return this;
    }

    public PatternTreeBuilder WithFastNoiseLite(FastNoiseLiteSettings settings)
    {
        _currentNode = new FastNoiseLiteNode(settings);
        return this;
    }

    public PatternTreeBuilder WithFastNoiseLite(FastNoiseLite fastNoiseLite)
    {
        _currentNode = new FastNoiseLiteNode(fastNoiseLite);
        return this;
    }

    public PatternTreeBuilder WithMathExpression(string mathExpression)
    {
        _currentNode = new MathExpressionNode(mathExpression);
        return this;
    }

    public PatternTreeBuilder WithMathExpression(PatternTreeNode node, string mathExpression)
    {
        _currentNode = new MathExpressionNode(node, mathExpression);
        return this;
    }

    public PatternTreeBuilder WithMathExpression(IEnumerable<PatternTreeNode> nodes, string mathExpression)
    {
        _currentNode = new MathExpressionNode(nodes, mathExpression);
        return this;
    }

    public PatternTreeBuilder ApplyOperation(Func<PatternTreeNode, SingleChildOperationNode> nodeConstructor)
    {
        _currentNode = nodeConstructor(_currentNode);
        return this;
    }

    public PatternTreeBuilder ApplyOperation(Func<PatternTreeNode, PositionTransformNode> nodeConstructor)
    {
        _currentNode = nodeConstructor(_currentNode);
        return this;
    }

    public PatternTreeBuilder ScaleXBy(double scale)
    {
        _currentNode = new PositionTransformNode(_currentNode, x: new PositionXNode().Multiply(1 / scale));
        return this;
    }

    public PatternTreeBuilder ScaleYBy(double scale)
    {
        _currentNode = new PositionTransformNode(_currentNode, y: new PositionYNode().Multiply(1 / scale));
        return this;
    }

    public PatternTreeBuilder ScaleZBy(double scale)
    {
        _currentNode = new PositionTransformNode(_currentNode, z: new PositionZNode().Multiply(1 / scale));
        return this;
    }

    public PatternTreeBuilder ScaleBy(double scaleX, double scaleY, double scaleZ)
    {
        _currentNode = new PositionTransformNode(_currentNode,
            x: new PositionXNode().Multiply(1 / scaleX),
            y: new PositionYNode().Multiply(1 / scaleY),
            z: new PositionZNode().Multiply(1 / scaleZ));
        return this;
    }

    public PatternTreeBuilder ApplyMathExpression(string mathExpression)
    {
        _currentNode = new MathExpressionNode(_currentNode, mathExpression);
        return this;
    }

    public PatternTreeBuilder ApplyOperation(PatternTreeNode secondNode,
        Func<PatternTreeNode, PatternTreeNode, DualChildOperationNode> nodeConstructor)
    {
        _currentNode = nodeConstructor(_currentNode, secondNode);
        return this;
    }

    public PatternTreeBuilder ApplyMathExpression(PatternTreeNode secondNode, string mathExpression)
    {
        _currentNode = new MathExpressionNode(new List<PatternTreeNode> { _currentNode, secondNode }, mathExpression);
        return this;
    }

    public PatternTreeBuilder ApplyOperation(IEnumerable<PatternTreeNode> nodes,
        Func<PatternTreeNode, IEnumerable<PatternTreeNode>, MultiChildOperationNode> nodeConstructor)
    {
        _currentNode = nodeConstructor(_currentNode, nodes);
        return this;
    }

    public PatternTreeBuilder ApplyMathExpression(IEnumerable<PatternTreeNode> additionalNodes, string mathExpression)
    {
        var allNodes = new List<PatternTreeNode> { _currentNode };
        allNodes.AddRange(additionalNodes);
        _currentNode = new MathExpressionNode(allNodes, mathExpression);
        return this;
    }

    public PatternTreeBuilder Add(double value)
    {
        _currentNode = _currentNode.Add(value);
        return this;
    }

    public PatternTreeBuilder Subtract(double value)
    {
        _currentNode = _currentNode.Subtract(value);
        return this;
    }

    public PatternTreeBuilder Multiply(double value)
    {
        _currentNode = _currentNode.Multiply(value);
        return this;
    }

    public PatternTreeBuilder Divide(double value)
    {
        _currentNode = _currentNode.Divide(value);
        return this;
    }

    public PatternTreeBuilder Mod(double value)
    {
        _currentNode = _currentNode.Mod(value);
        return this;
    }

    public PatternTreeBuilder Power(double value)
    {
        _currentNode = _currentNode.Power(value);
        return this;
    }

    public PatternTreeBuilder Min(double value)
    {
        _currentNode = _currentNode.Min(value);
        return this;
    }

    public PatternTreeBuilder Max(double value)
    {
        _currentNode = _currentNode.Max(value);
        return this;
    }

    public PatternTreeBuilder Add(PatternTreeNode node)
    {
        _currentNode = _currentNode.Add(node);
        return this;
    }

    public PatternTreeBuilder Subtract(PatternTreeNode node)
    {
        _currentNode = _currentNode.Subtract(node);
        return this;
    }

    public PatternTreeBuilder Multiply(PatternTreeNode node)
    {
        _currentNode = _currentNode.Multiply(node);
        return this;
    }

    public PatternTreeBuilder Divide(PatternTreeNode node)
    {
        _currentNode = _currentNode.Divide(node);
        return this;
    }

    public PatternTreeBuilder Mod(PatternTreeNode node)
    {
        _currentNode = _currentNode.Mod(node);
        return this;
    }

    public PatternTreeBuilder Power(PatternTreeNode node)
    {
        _currentNode = _currentNode.Power(node);
        return this;
    }

    public PatternTreeBuilder Min(PatternTreeNode node)
    {
        _currentNode = _currentNode.Min(node);
        return this;
    }

    public PatternTreeBuilder Max(PatternTreeNode node)
    {
        _currentNode = _currentNode.Max(node);
        return this;
    }

    public PatternTreeNode BuildNode()
    {
        return _currentNode;
    }

    public PatternTree Build()
    {
        if (_currentNode == null)
            throw new InvalidOperationException(
                "Cannot build a pattern tree without any nodes. Start with a node builder method.");

        if (string.IsNullOrEmpty(_id) || string.IsNullOrEmpty(_name))
            throw new InvalidOperationException(
                "Pattern ID and name must be set using WithIdentifier() before building.");

        return new PatternTree(_id, _name, _currentNode);
    }
}