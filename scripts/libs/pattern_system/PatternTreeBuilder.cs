using System;
using System.Collections.Generic;

namespace PatternSystem;

public class PatternTreeBuilder
{
    private PatternTreeNode _currentNode;
    private string _id;
    private string _name;

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

    public PatternTreeBuilder WithFastNoiseLite(Godot.FastNoiseLite fastNoiseLite)
    {
        _currentNode = new FastNoiseLiteNode(fastNoiseLite);
        return this;
    }

    public PatternTreeBuilder ApplySingleOperation<T>(Func<PatternTreeNode, T> nodeConstructor)
        where T : SingleChildOperationNode
    {
        _currentNode = nodeConstructor(_currentNode);
        return this;
    }

    public PatternTreeBuilder ApplyDualOperation<T>(PatternTreeNode secondNode, Func<PatternTreeNode, PatternTreeNode, T> nodeConstructor)
        where T : DualChildOperationNode
    {
        _currentNode = nodeConstructor(_currentNode, secondNode);
        return this;
    }

    public PatternTreeBuilder ApplyMultiOperation<T>(IEnumerable<PatternTreeNode> nodes, Func<IEnumerable<PatternTreeNode>, T> nodeConstructor)
        where T : MultiChildOperationNode
    {
        _currentNode = nodeConstructor(nodes);
        return this;
    }

    public PatternTree Build()
    {
        if (_currentNode == null)
        {
            throw new InvalidOperationException("Cannot build a pattern tree without any nodes. Start with a node builder method.");
        }

        if (string.IsNullOrEmpty(_id) || string.IsNullOrEmpty(_name))
        {
            throw new InvalidOperationException("Pattern ID and name must be set using WithIdentifier() before building.");
        }

        return new PatternTree(_id, _name, _currentNode);
    }
}