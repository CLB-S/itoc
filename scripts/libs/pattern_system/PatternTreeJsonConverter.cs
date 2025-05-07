using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace PatternSystem;

/// <summary>
///     Handles serialization and deserialization of PatternTreeNode objects to/from JSON.
/// </summary>
public class PatternTreeJsonConverter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new PatternTreeNodeJsonConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    ///     Serializes a PatternTreeNode to a JSON string.
    /// </summary>
    public static string Serialize(PatternTreeNode node)
    {
        return JsonSerializer.Serialize(node, SerializerOptions);
    }

    /// <summary>
    ///     Deserializes a JSON string into a PatternTreeNode.
    /// </summary>
    public static PatternTreeNode Deserialize(string json)
    {
        return JsonSerializer.Deserialize<PatternTreeNode>(json, SerializerOptions);
    }
}

/// <summary>
///     Custom JsonConverter for PatternTreeNode objects.
/// </summary>
public class PatternTreeNodeJsonConverter : JsonConverter<PatternTreeNode>
{
    private const string TypePropertyName = "Type";
    private const string PropertiesPropertyName = "Properties";
    private const string ChildrenPropertyName = "Children";
    private const string IdPropertyName = "Id";
    private const string NamePropertyName = "Name";
    private const string RootNodePropertyName = "RootNode";

    public override PatternTreeNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object");

        string nodeType = null;
        Dictionary<string, JsonElement> properties = new();
        List<PatternTreeNode> children = new();
        string id = null;
        string name = null;
        PatternTreeNode rootNode = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            var propertyName = reader.GetString();

            reader.Read(); // Move to property value

            switch (propertyName)
            {
                case TypePropertyName:
                    nodeType = reader.GetString();
                    break;
                case IdPropertyName:
                    id = reader.GetString();
                    break;
                case NamePropertyName:
                    name = reader.GetString();
                    break;
                case RootNodePropertyName:
                    rootNode = Read(ref reader, typeof(PatternTreeNode), options);
                    break;
                case PropertiesPropertyName:
                    if (reader.TokenType != JsonTokenType.StartObject)
                        throw new JsonException("Expected properties object");

                    // Parse properties as key-value pairs
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName)
                            continue;

                        var propName = reader.GetString();
                        reader.Read();
                        properties[propName] = JsonDocument.ParseValue(ref reader).RootElement;
                    }

                    break;
                case ChildrenPropertyName:
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected children array");

                    // Parse children array
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        children.Add(Read(ref reader, typeof(PatternTreeNode), options));
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        // Handle PatternTree
        if (nodeType == nameof(PatternTree))
        {
            if (id == null || name == null || rootNode == null)
                throw new JsonException("PatternTree requires Id, Name and RootNode properties");

            return new PatternTree(id, name, rootNode);
        }

        // Handle other node types
        return CreateNodeFromJson(nodeType, properties, children);
    }

    public override void Write(Utf8JsonWriter writer, PatternTreeNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Handle PatternTree specially
        if (value is PatternTree patternTree)
        {
            writer.WriteString(TypePropertyName, nameof(PatternTree));
            writer.WriteString(IdPropertyName, patternTree.Id);
            writer.WriteString(NamePropertyName, patternTree.Name);

            writer.WritePropertyName(RootNodePropertyName);
            Write(writer, patternTree.RootNode, options);
        }
        else
        {
            // Write node type
            writer.WriteString(TypePropertyName, value?.GetType().Name);

            if (!(value is PositionXNode ||
                  value is PositionYNode ||
                  value is PositionZNode ||
                  value is PositionTransformNode))
            {
                // Write node-specific properties
                writer.WritePropertyName(PropertiesPropertyName);
                writer.WriteStartObject();
                WriteNodeProperties(writer, value);
                writer.WriteEndObject();
            }

            // Write children if applicable
            if (value is IOperator operatorNode)
            {
                writer.WritePropertyName(ChildrenPropertyName);
                writer.WriteStartArray();
                foreach (var child in operatorNode.Children) Write(writer, child, options);
                writer.WriteEndArray();
            }
        }

        writer.WriteEndObject();
    }

    private void WriteNodeProperties(Utf8JsonWriter writer, PatternTreeNode node)
    {
        switch (node)
        {
            case ConstantNode constantNode:
                writer.WriteNumber("Value", constantNode.Value);
                break;

            case FastNoiseLiteNode noiseNode:
                var settings = noiseNode.Settings;
                var defaultSettings = new FastNoiseLiteSettings();

                // Only write values that differ from defaults
                if (settings.CellularDistanceFunction != defaultSettings.CellularDistanceFunction)
                    writer.WriteString("CellularDistanceFunction", settings.CellularDistanceFunction.ToString());

                if (settings.CellularJitter != defaultSettings.CellularJitter)
                    writer.WriteNumber("CellularJitter", settings.CellularJitter);

                if (settings.CellularReturnType != defaultSettings.CellularReturnType)
                    writer.WriteString("CellularReturnType", settings.CellularReturnType.ToString());

                if (settings.DomainWarpAmplitude != defaultSettings.DomainWarpAmplitude)
                    writer.WriteNumber("DomainWarpAmplitude", settings.DomainWarpAmplitude);

                if (settings.DomainWarpEnabled != defaultSettings.DomainWarpEnabled)
                    writer.WriteBoolean("DomainWarpEnabled", settings.DomainWarpEnabled);

                if (settings.DomainWarpFractalGain != defaultSettings.DomainWarpFractalGain)
                    writer.WriteNumber("DomainWarpFractalGain", settings.DomainWarpFractalGain);

                if (settings.DomainWarpFractalLacunarity != defaultSettings.DomainWarpFractalLacunarity)
                    writer.WriteNumber("DomainWarpFractalLacunarity", settings.DomainWarpFractalLacunarity);

                if (settings.DomainWarpFractalOctaves != defaultSettings.DomainWarpFractalOctaves)
                    writer.WriteNumber("DomainWarpFractalOctaves", settings.DomainWarpFractalOctaves);

                if (settings.DomainWarpFractalType != defaultSettings.DomainWarpFractalType)
                    writer.WriteString("DomainWarpFractalType", settings.DomainWarpFractalType.ToString());

                if (settings.DomainWarpFrequency != defaultSettings.DomainWarpFrequency)
                    writer.WriteNumber("DomainWarpFrequency", settings.DomainWarpFrequency);

                if (settings.DomainWarpType != defaultSettings.DomainWarpType)
                    writer.WriteString("DomainWarpType", settings.DomainWarpType.ToString());

                if (settings.FractalGain != defaultSettings.FractalGain)
                    writer.WriteNumber("FractalGain", settings.FractalGain);

                if (settings.FractalLacunarity != defaultSettings.FractalLacunarity)
                    writer.WriteNumber("FractalLacunarity", settings.FractalLacunarity);

                if (settings.FractalOctaves != defaultSettings.FractalOctaves)
                    writer.WriteNumber("FractalOctaves", settings.FractalOctaves);

                if (settings.FractalPingPongStrength != defaultSettings.FractalPingPongStrength)
                    writer.WriteNumber("FractalPingPongStrength", settings.FractalPingPongStrength);

                if (settings.FractalType != defaultSettings.FractalType)
                    writer.WriteString("FractalType", settings.FractalType.ToString());

                if (settings.FractalWeightedStrength != defaultSettings.FractalWeightedStrength)
                    writer.WriteNumber("FractalWeightedStrength", settings.FractalWeightedStrength);

                if (settings.Frequency != defaultSettings.Frequency)
                    writer.WriteNumber("Frequency", settings.Frequency);

                if (settings.NoiseType != defaultSettings.NoiseType)
                    writer.WriteString("NoiseType", settings.NoiseType.ToString());

                if (settings.Seed != defaultSettings.Seed)
                    writer.WriteNumber("Seed", settings.Seed);

                // Only write offset if it's not zero
                if (settings.Offset != defaultSettings.Offset)
                {
                    writer.WriteStartObject("Offset");
                    writer.WriteNumber("X", settings.Offset.X);
                    writer.WriteNumber("Y", settings.Offset.Y);
                    writer.WriteNumber("Z", settings.Offset.Z);
                    writer.WriteEndObject();
                }

                break;

            case MultiChildOperationNode multiNode:
                writer.WriteString("OperationType", multiNode.OperationType.ToString());
                break;

            case DualChildOperationNode dualNode:
                writer.WriteString("OperationType", dualNode.OperationType.ToString());
                break;

            case SingleChildOperationNode singleNode:
                writer.WriteString("OperationType", singleNode.OperationType.ToString());
                break;

            case MathExpressionNode mathNode:
                writer.WriteString("MathExpression", mathNode.MathExpression);
                break;
        }
    }

    private PatternTreeNode CreateNodeFromJson(string nodeType, Dictionary<string, JsonElement> properties,
        List<PatternTreeNode> children)
    {
        switch (nodeType)
        {
            case null:
                return null;

            case nameof(ConstantNode):
                var value = properties["Value"].GetDouble();
                return new ConstantNode(value);

            case nameof(PositionXNode):
                return new PositionXNode();

            case nameof(PositionYNode):
                return new PositionYNode();

            case nameof(PositionZNode):
                return new PositionZNode();

            case nameof(PositionTransformNode):
                return new PositionTransformNode(
                    children.Count > 0 ? children[0] : null,
                    children.Count > 1 ? children[1] : null,
                    children.Count > 2 ? children[2] : null,
                    children.Count > 3 ? children[3] : null
                );

            case nameof(FastNoiseLiteNode):
                // Create settings with defaults
                var settings = new FastNoiseLiteSettings();

                // Override with values from JSON if they exist
                foreach (var prop in properties)
                    switch (prop.Key)
                    {
                        case "CellularDistanceFunction":
                            settings.CellularDistanceFunction =
                                Enum.Parse<CellularDistanceFunction>(prop.Value.GetString());
                            break;
                        case "CellularJitter":
                            settings.CellularJitter = prop.Value.GetDouble();
                            break;
                        case "CellularReturnType":
                            settings.CellularReturnType = Enum.Parse<CellularReturnType>(prop.Value.GetString());
                            break;
                        case "DomainWarpAmplitude":
                            settings.DomainWarpAmplitude = prop.Value.GetDouble();
                            break;
                        case "DomainWarpEnabled":
                            settings.DomainWarpEnabled = prop.Value.GetBoolean();
                            break;
                        case "DomainWarpFractalGain":
                            settings.DomainWarpFractalGain = prop.Value.GetDouble();
                            break;
                        case "DomainWarpFractalLacunarity":
                            settings.DomainWarpFractalLacunarity = prop.Value.GetDouble();
                            break;
                        case "DomainWarpFractalOctaves":
                            settings.DomainWarpFractalOctaves = prop.Value.GetInt32();
                            break;
                        case "DomainWarpFractalType":
                            settings.DomainWarpFractalType = Enum.Parse<DomainWarpFractalType>(prop.Value.GetString());
                            break;
                        case "DomainWarpFrequency":
                            settings.DomainWarpFrequency = prop.Value.GetDouble();
                            break;
                        case "DomainWarpType":
                            settings.DomainWarpType = Enum.Parse<DomainWarpType>(prop.Value.GetString());
                            break;
                        case "FractalGain":
                            settings.FractalGain = prop.Value.GetDouble();
                            break;
                        case "FractalLacunarity":
                            settings.FractalLacunarity = prop.Value.GetDouble();
                            break;
                        case "FractalOctaves":
                            settings.FractalOctaves = prop.Value.GetInt32();
                            break;
                        case "FractalPingPongStrength":
                            settings.FractalPingPongStrength = prop.Value.GetDouble();
                            break;
                        case "FractalType":
                            settings.FractalType = Enum.Parse<FractalType>(prop.Value.GetString());
                            break;
                        case "FractalWeightedStrength":
                            settings.FractalWeightedStrength = prop.Value.GetDouble();
                            break;
                        case "Frequency":
                            settings.Frequency = prop.Value.GetDouble();
                            break;
                        case "NoiseType":
                            settings.NoiseType = Enum.Parse<NoiseType>(prop.Value.GetString());
                            break;
                        case "Seed":
                            settings.Seed = prop.Value.GetInt32();
                            break;
                        case "Offset":
                            settings.Offset = new Vector3(
                                prop.Value.GetProperty("X").GetDouble(),
                                prop.Value.GetProperty("Y").GetDouble(),
                                prop.Value.GetProperty("Z").GetDouble()
                            );
                            break;
                    }

                return new FastNoiseLiteNode(settings);

            case nameof(MultiChildOperationNode):
                var multiOpType = Enum.Parse<MultiOperationType>(properties["OperationType"].GetString());
                return new MultiChildOperationNode(children, multiOpType);

            case nameof(DualChildOperationNode):
                var dualOpType = Enum.Parse<DualOperationType>(properties["OperationType"].GetString());
                return new DualChildOperationNode(
                    children.Count > 0 ? children[0] : null,
                    children.Count > 1 ? children[1] : null,
                    dualOpType
                );

            case nameof(SingleChildOperationNode):
                var singleOpType = Enum.Parse<SingleOperationType>(properties["OperationType"].GetString());
                return new SingleChildOperationNode(
                    children.Count > 0 ? children[0] : null,
                    singleOpType
                );

            case nameof(MathExpressionNode):
                var mathExpression = properties["MathExpression"].GetString();
                return children.Count > 0
                    ? new MathExpressionNode(children, mathExpression)
                    : new MathExpressionNode(mathExpression);

            default:
                throw new JsonException($"Unknown node type: {nodeType}");
        }
    }
}