using System;
using System.IO;
using System.Linq;
using Godot;

namespace PatternSystem;

/// <summary>
///     Example class demonstrating how to use the PatternTreeJsonConverter and PatternTreeJsonUtility classes.
/// </summary>
public class PatternTreeJsonExample
{
    /// <summary>
    ///     Creates a sample pattern tree for demonstration purposes.
    /// </summary>
    public static PatternTreeNode CreateSamplePatternTree()
    {
        // Create a noise generator with default settings
        var noiseSettings = new FastNoiseLiteSettings
        {
            NoiseType = NoiseType.Perlin,
            Frequency = 0.02,
            FractalType = FractalType.Fbm,
            FractalOctaves = 4,
            Seed = 42
        };
        var noiseNode = new FastNoiseLiteNode(noiseSettings);

        // Create a constant node
        var constantNode = new ConstantNode(0.5);

        // Create a dual operation to combine them
        var dualNode = new DualChildOperationNode(noiseNode, constantNode);

        // Apply a single operation to the result
        var singleNode = new SingleChildOperationNode(dualNode, SingleOperationType.Clamp);

        // Return the complete pattern tree
        return singleNode;
    }

    /// <summary>
    ///     Demonstrates how to save a pattern tree to a JSON string and load it back.
    /// </summary>
    public void DemonstrateJsonSerialization()
    {
        var tree = CreateSamplePatternTree();

        // Serialize to JSON string
        var json = PatternTreeJsonConverter.Serialize(tree);
        GD.Print("Serialized JSON:");
        GD.Print(json);

        // Deserialize back to pattern tree
        var loadedTree = PatternTreeJsonConverter.Deserialize(json);

        // Demonstrate that the loaded tree gives the same results as the original
        double x = 10.5, y = 20.3;
        var originalValue = tree.Evaluate(x, y);
        var loadedValue = loadedTree.Evaluate(x, y);

        GD.Print($"Original tree evaluation: {originalValue}");
        GD.Print($"Loaded tree evaluation: {loadedValue}");
        GD.Print($"Values match: {Math.Abs(originalValue - loadedValue) < 0.0001}");
    }

    /// <summary>
    ///     Demonstrates how to save a pattern tree to a file and load it back.
    /// </summary>
    public void DemonstrateFileSerialization(string directory = "user://")
    {
        var tree = CreateSamplePatternTree();
        var filePath = Path.Combine(directory, "sample_pattern_tree.json");

        // Save to file
        PatternTreeJsonUtility.SaveToFile(tree, filePath);
        GD.Print($"Pattern tree saved to: {filePath}");

        // Load from file
        var loadedTree = PatternTreeJsonUtility.LoadFromFile(filePath);

        // Demonstrate that the loaded tree gives the same results as the original
        double x = 10.5, y = 20.3;
        var originalValue = tree.Evaluate(x, y);
        var loadedValue = loadedTree.Evaluate(x, y);

        GD.Print($"Original tree evaluation: {originalValue}");
        GD.Print($"Loaded tree evaluation: {loadedValue}");
        GD.Print($"Values match: {Math.Abs(originalValue - loadedValue) < 0.0001}");
    }

    /// <summary>
    ///     Demonstrates how to save a pattern tree to a Godot resource file and load it back.
    /// </summary>
    public void DemonstrateGodotResourceSerialization(string directory = "user://")
    {
        var tree = CreateSamplePatternTree();
        var filePath = Path.Combine(directory, "sample_pattern_tree.tres");

        // Save to Godot resource
        PatternTreeJsonUtility.SaveToGodotResource(tree, filePath);
        GD.Print($"Pattern tree saved to Godot resource: {filePath}");

        // Load from Godot resource
        var loadedTree = PatternTreeJsonUtility.LoadFromGodotResource(filePath);

        // Demonstrate that the loaded tree gives the same results as the original
        double x = 10.5, y = 20.3, z = 5.2;
        var originalValue = tree.Evaluate(x, y, z);
        var loadedValue = loadedTree.Evaluate(x, y, z);

        GD.Print($"Original tree evaluation (3D): {originalValue}");
        GD.Print($"Loaded tree evaluation (3D): {loadedValue}");
        GD.Print($"Values match: {Math.Abs(originalValue - loadedValue) < 0.0001}");
    }

    /// <summary>
    ///     Demonstrates serializing a more complex pattern tree with multiple levels.
    /// </summary>
    public void DemonstrateComplexPatternTree()
    {
        // Create two different noise generators
        var noiseSettings1 = new FastNoiseLiteSettings
        {
            NoiseType = NoiseType.Perlin,
            Frequency = 0.02,
            FractalType = FractalType.Fbm,
            Seed = 42
        };
        var noiseNode1 = new FastNoiseLiteNode(noiseSettings1);

        var noiseSettings2 = new FastNoiseLiteSettings
        {
            NoiseType = NoiseType.Cellular,
            Frequency = 0.03,
            FractalType = FractalType.Ridged,
            Seed = 123
        };
        var noiseNode2 = new FastNoiseLiteNode(noiseSettings2);

        // Create a math expression to combine them
        var mathNode = new MathExpressionNode([noiseNode1, noiseNode2], "x1 * 0.7 + x2 * 0.3");

        // Apply a multi-child operation with additional constant nodes
        var multiNode = new MultiChildOperationNode(
            [
                mathNode,
                new ConstantNode(0.2),
                new ConstantNode(0.3)
            ]
        );

        // Serialize to JSON
        var json = PatternTreeJsonConverter.Serialize(multiNode);
        GD.Print("Complex tree JSON:");
        GD.Print(json);

        // Deserialize and test
        var loadedTree = PatternTreeJsonConverter.Deserialize(json);
        double x = 15.7, y = 22.3, z = 8.9;
        var originalValue = multiNode.Evaluate(x, y, z);
        var loadedValue = loadedTree.Evaluate(x, y, z);

        GD.Print($"Complex original evaluation: {originalValue}");
        GD.Print($"Complex loaded evaluation: {loadedValue}");
        GD.Print($"Values match: {Math.Abs(originalValue - loadedValue) < 0.0001}");
    }

    /// <summary>
    ///     Demonstrates how the improved JSON converter handles enum names and non-default values.
    /// </summary>
    public void DemonstrateImprovedJsonFormat()
    {
        // Create a noise node with several modified settings
        var noiseSettings = new FastNoiseLiteSettings
        {
            NoiseType = NoiseType.Perlin,
            Frequency = 0.02,
            FractalType = FractalType.Fbm,
            FractalOctaves = 4,
            Seed = 42
        };
        var noiseNode = new FastNoiseLiteNode(noiseSettings);

        // Serialize to JSON
        var json = PatternTreeJsonConverter.Serialize(noiseNode);
        GD.Print("Improved JSON format (enum names and non-default values only):");
        GD.Print(json);

        // Create a pattern tree with different operation types
        var constantNode = new ConstantNode(0.5);
        var addNode = new DualChildOperationNode(noiseNode, constantNode);
        var multiNode = new MultiChildOperationNode(
            [noiseNode, constantNode, new ConstantNode(0.25)],
            MultiOperationType.Average
        );
        var singleNode = new SingleChildOperationNode(multiNode, SingleOperationType.Clamp);

        // Serialize complex tree
        var complexJson = PatternTreeJsonConverter.Serialize(singleNode);
        GD.Print("\nComplex tree with operation enum names:");
        GD.Print(complexJson);

        // Demonstrate compact JSON with default values
        var defaultSettings = new FastNoiseLiteSettings();
        var defaultNode = new FastNoiseLiteNode(defaultSettings);
        var defaultJson = PatternTreeJsonConverter.Serialize(defaultNode);
        GD.Print("\nNode with default settings (minimal JSON):");
        GD.Print(defaultJson);
    }

    /// <summary>
    ///     Demonstrates serializing a pattern tree to a file and loading it back.
    ///     Also validates that the deserialization correctly parses enum names.
    /// </summary>
    public void DemonstrateEnumSerializationAndParsing()
    {
        // Create a tree with various operation types
        var constantNode = new ConstantNode(0.75);

        var singleNode = new SingleChildOperationNode(constantNode, SingleOperationType.Square);

        var dualNode = new DualChildOperationNode(
            singleNode,
            new ConstantNode(0.25),
            DualOperationType.Max
        );

        var multiNode = new MultiChildOperationNode(
            [dualNode, new ConstantNode(0.33), new ConstantNode(0.66)],
            MultiOperationType.Min
        );

        // Serialize to JSON
        var json = PatternTreeJsonConverter.Serialize(multiNode);
        GD.Print("Tree with various enum operation types:");
        GD.Print(json);

        // Deserialize back
        var loadedTree = PatternTreeJsonConverter.Deserialize(json);

        // Validate enum values were preserved
        if (loadedTree is MultiChildOperationNode loadedMultiNode)
        {
            GD.Print($"Deserialized MultiOperationType: {loadedMultiNode.OperationType}");

            // Check child nodes' operation types too
            if (loadedMultiNode.Children.FirstOrDefault() is DualChildOperationNode loadedDualNode)
            {
                GD.Print($"Deserialized DualOperationType: {loadedDualNode.OperationType}");

                if (loadedDualNode.Children.FirstOrDefault() is SingleChildOperationNode loadedSingleNode)
                    GD.Print($"Deserialized SingleOperationType: {loadedSingleNode.OperationType}");
            }
        }
    }

    /// <summary>
    ///     Demonstrates how to create, serialize, and deserialize a PatternTree object.
    /// </summary>
    public void DemonstratePatternTreeSerialization()
    {
        // Create a sample pattern tree
        var sampleTree = CreateSamplePatternTree();

        // Wrap it in a PatternTree with ID and name
        var patternTree = new PatternTree("terrain_height", "Terrain Height Pattern", sampleTree);

        // Serialize to JSON
        var json = PatternTreeJsonConverter.Serialize(patternTree);
        GD.Print("PatternTree JSON:");
        GD.Print(json);

        // Deserialize back
        var loadedNode = PatternTreeJsonConverter.Deserialize(json);

        if (loadedNode is PatternTree loadedPatternTree)
        {
            GD.Print("\nDeserialized PatternTree properties:");
            GD.Print($"ID: {loadedPatternTree.Id}");
            GD.Print($"Name: {loadedPatternTree.Name}");

            // Demonstrate that the tree correctly evaluates
            double x = 12.3, y = 45.6, z = 7.8;
            var originalValue = patternTree.Evaluate(x, y, z);
            var loadedValue = loadedPatternTree.Evaluate(x, y, z);

            GD.Print($"Original evaluation: {originalValue}");
            GD.Print($"Loaded evaluation: {loadedValue}");
            GD.Print($"Values match: {Math.Abs(originalValue - loadedValue) < 0.0001}");
        }
        else
        {
            GD.PrintErr("Failed to deserialize PatternTree");
        }
    }

    /// <summary>
    ///     Demonstrates how to save multiple PatternTree objects to a directory and load them back.
    /// </summary>
    public void DemonstratePatternTreeDirectoryOperations(string directory = "user://patterns")
    {
        // Create directory if it doesn't exist
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        // Create and save several pattern trees
        var patternTrees = new PatternTree[]
        {
            new(
                "terrain_height",
                "Terrain Height Pattern",
                CreateSamplePatternTree()
            ),
            new(
                "temperature",
                "Temperature Pattern",
                new MathExpressionNode(
                    new FastNoiseLiteNode(new FastNoiseLiteSettings
                    {
                        NoiseType = NoiseType.Perlin,
                        Frequency = 0.01,
                        Seed = 123
                    }),
                    "x * 0.5 + 0.5" // Normalize to 0-1 range
                )
            ),
            new(
                "moisture",
                "Moisture Pattern",
                new DualChildOperationNode(
                    new FastNoiseLiteNode(new FastNoiseLiteSettings
                    {
                        NoiseType = NoiseType.Simplex,
                        Frequency = 0.02,
                        Seed = 456
                    }),
                    new ConstantNode(0.2)
                )
            )
        };

        // Save all pattern trees
        foreach (var pattern in patternTrees)
        {
            var filePath = PatternTreeJsonUtility.SavePatternTree(pattern, directory);
            GD.Print($"Saved pattern '{pattern.Name}' to {filePath}");
        }

        // Load all pattern trees back
        GD.Print("\nLoading all pattern trees from directory:");
        var loadedPatterns = PatternTreeJsonUtility.LoadAllPatternTrees(directory);

        // Display the loaded patterns
        foreach (var pattern in loadedPatterns.Where(p => p != null))
        {
            GD.Print($"Loaded pattern: ID={pattern.Id}, Name='{pattern.Name}'");

            // Sample value
            var value = pattern.Evaluate(10.0, 10.0, 10.0);
            GD.Print($"  Sample value at (10,10,10): {value}");
        }
    }

    public PatternTreeJsonExample()
    {
        GD.Print("Pattern Tree JSON Example");
        DemonstrateJsonSerialization();

        GD.Print("\n=== Improved JSON Format Test ===");
        DemonstrateImprovedJsonFormat();

        GD.Print("\n=== Enum Serialization and Parsing Test ===");
        DemonstrateEnumSerializationAndParsing();

        GD.Print("\n=== PatternTree Serialization Test ===");
        DemonstratePatternTreeSerialization();

        // Uncomment to test directory operations
        // GD.Print("\n=== PatternTree Directory Operations Test ===");
        // DemonstratePatternTreeDirectoryOperations();

        // Uncomment to test file serialization
        // DemonstrateFileSerialization();
        // DemonstrateGodotResourceSerialization();
        // DemonstrateComplexPatternTree();
    }
}