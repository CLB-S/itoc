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

    public PatternTreeBuilder ApplyOperation<T>(Func<PatternTreeNode, T> nodeConstructor)
        where T : SingleChildOperationNode
    {
        _currentNode = nodeConstructor(_currentNode);
        return this;
    }

    public PatternTreeBuilder ApplyMathExpression(string mathExpression)
    {
        _currentNode = new MathExpressionNode(_currentNode, mathExpression);
        return this;
    }

    public PatternTreeBuilder ApplyOperation<T>(PatternTreeNode secondNode,
        Func<PatternTreeNode, PatternTreeNode, T> nodeConstructor)
        where T : DualChildOperationNode
    {
        _currentNode = nodeConstructor(_currentNode, secondNode);
        return this;
    }

    public PatternTreeBuilder ApplyMathExpression(PatternTreeNode secondNode, string mathExpression)
    {
        _currentNode = new MathExpressionNode(new List<PatternTreeNode> { _currentNode, secondNode }, mathExpression);
        return this;
    }

    public PatternTreeBuilder ApplyOperation<T>(IEnumerable<PatternTreeNode> nodes,
        Func<PatternTreeNode, IEnumerable<PatternTreeNode>, T> nodeConstructor)
        where T : MultiChildOperationNode
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