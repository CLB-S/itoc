namespace ITOC.Core.Command;

/// <summary>
/// Represents a command argument with a type and validation
/// </summary>
public class CommandArgument
{
    /// <summary>
    /// Name of the argument
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Description of the argument
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Override for suggestions.
    /// </summary>
    public string[] SuggestionsOverride { get; }

    /// <summary>
    /// Type of the argument
    /// </summary>
    public IArgumentType Type { get; }

    /// <summary>
    /// Whether this argument is required
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Default value for this argument if not provided
    /// </summary>
    public object DefaultValue { get; }

    /// <summary>
    /// Creates a new command argument
    /// </summary>
    /// <param name="name">Name of the argument</param>
    /// <param name="type">Type of the argument</param>
    /// <param name="description">Description of the argument</param>
    /// <param name="isRequired">Whether this argument is required</param>
    /// <param name="defaultValue">Default value if argument is not provided</param>
    public CommandArgument(
        string name,
        IArgumentType type,
        string description = "",
        string[] suggestionsOverride = null,
        bool isRequired = true,
        object defaultValue = null
    )
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Description = description ?? "";
        SuggestionsOverride = suggestionsOverride;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Returns a list of suggestions for this argument type based on the current input
    /// </summary>
    /// <param name="currentInput">The current input string</param>
    /// <param name="context">Optional context object for contextual suggestions</param>
    /// <returns>A list of suggested values</returns>
    public IEnumerable<string> GetSuggestions(string currentInput, object context = null)
    {
        if (SuggestionsOverride != null && SuggestionsOverride.Length > 0)
            return SuggestionsOverride;

        return Type.GetSuggestions(Name, currentInput, context);
    }
}

/// <summary>
/// Interface for command argument types that can parse and validate input
/// </summary>
public interface IArgumentType
{
    /// <summary>
    /// Gets the name of this argument type
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Parses a string input into the appropriate type
    /// </summary>
    /// <param name="input">The input string to parse</param>
    /// <param name="result">The parsed result, if successful</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    bool TryParse(string input, out object result);

    /// <summary>
    /// Returns a list of suggestions for this argument type based on the current input
    /// </summary>
    /// <param name="currentInput">The current input string</param>
    /// <param name="context">Optional context object for contextual suggestions</param>
    /// <returns>A list of suggested values</returns>
    IEnumerable<string> GetSuggestions(
        string argumentName,
        string currentInput,
        object context = null
    );
}
