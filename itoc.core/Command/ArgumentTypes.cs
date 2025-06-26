namespace ITOC.Core.Command;

/// <summary>
/// Base class for argument types
/// </summary>
public abstract class ArgumentTypeBase : IArgumentType
{
    public abstract string TypeName { get; }
    public abstract bool TryParse(string input, out object result);

    public virtual IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    ) => Enumerable.Empty<string>();
}

/// <summary>
/// Argument type for integer values
/// </summary>
public class IntegerArgumentType : ArgumentTypeBase
{
    private readonly int? _min;
    private readonly int? _max;

    /// <summary>
    /// Creates a new integer argument type with optional range constraints
    /// </summary>
    /// <param name="min">Minimum allowed value (inclusive)</param>
    /// <param name="max">Maximum allowed value (inclusive)</param>
    public IntegerArgumentType(int? min = null, int? max = null)
    {
        _min = min;
        _max = max;
    }

    public override string TypeName => "integer";

    public override bool TryParse(string input, out object result)
    {
        result = null;

        if (!int.TryParse(input, out var value))
            return false;

        if (_min.HasValue && value < _min.Value)
            return false;

        if (_max.HasValue && value > _max.Value)
            return false;

        result = value;
        return true;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    )
    {
        if (_min == null && _max == null)
            return [$"<{argName}:int>"];
        return [$"<{argName}:int {_min?.ToString()}~{_max?.ToString()}>"];
    }
}

/// <summary>
/// Argument type for floating-point values
/// </summary>
public class FloatArgumentType : ArgumentTypeBase
{
    private readonly float? _min;
    private readonly float? _max;

    /// <summary>
    /// Creates a new float argument type with optional range constraints
    /// </summary>
    /// <param name="min">Minimum allowed value (inclusive)</param>
    /// <param name="max">Maximum allowed value (inclusive)</param>
    public FloatArgumentType(float? min = null, float? max = null)
    {
        _min = min;
        _max = max;
    }

    public override string TypeName => "float";

    public override bool TryParse(string input, out object result)
    {
        result = null;

        if (!float.TryParse(input, out var value))
            return false;

        if (_min.HasValue && value < _min.Value)
            return false;

        if (_max.HasValue && value > _max.Value)
            return false;

        result = value;
        return true;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    )
    {
        if (_min == null && _max == null)
            return [$"<{argName}:float>"];
        return [$"<{argName}:float {_min?.ToString()}~{_max?.ToString()}>"];
    }
}

/// <summary>
/// Argument type for boolean values (true/false)
/// </summary>
public class BoolArgumentType : ArgumentTypeBase
{
    private static readonly string[] _trueValues = ["true", "yes", "y", "1"];
    private static readonly string[] _falseValues = ["false", "no", "n", "0"];
    private static readonly string[] _allValues = _trueValues.Concat(_falseValues).ToArray();

    public override string TypeName => "boolean";

    public override bool TryParse(string input, out object result)
    {
        result = null;

        if (string.IsNullOrEmpty(input))
            return false;

        var lower = input.ToLowerInvariant();

        if (_trueValues.Contains(lower))
        {
            result = true;
            return true;
        }

        if (_falseValues.Contains(lower))
        {
            result = false;
            return true;
        }

        return false;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    )
    {
        if (string.IsNullOrEmpty(currentInput))
            return _allValues;

        var lower = currentInput.ToLowerInvariant();
        return _allValues.Where(v => v.StartsWith(lower, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Argument type for string values
/// </summary>
public class StringArgumentType : ArgumentTypeBase
{
    public override string TypeName => "string";

    public override bool TryParse(string input, out object result)
    {
        result = input ?? "";
        return true;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    ) => [$"<{argName}:str>"];
}

/// <summary>
/// Argument type for enumeration values
/// </summary>
public class EnumArgumentType<T> : ArgumentTypeBase
    where T : Enum
{
    private readonly Dictionary<string, T> _valueMap;
    private readonly string[] _names;

    public EnumArgumentType()
    {
        _valueMap = Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(
                e => e.ToString().ToLowerInvariant(),
                e => e,
                StringComparer.OrdinalIgnoreCase
            );
        _names = _valueMap.Keys.ToArray();
    }

    public override string TypeName => $"enum<{typeof(T).Name}>";

    public override bool TryParse(string input, out object result)
    {
        result = null;

        if (string.IsNullOrEmpty(input))
            return false;

        var lower = input.ToLowerInvariant();
        if (_valueMap.TryGetValue(lower, out var value))
        {
            result = value;
            return true;
        }

        return false;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    )
    {
        if (string.IsNullOrEmpty(currentInput))
            return _names;

        var lower = currentInput.ToLowerInvariant();
        return _names.Where(n => n.StartsWith(lower, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Argument type for selecting from a list of options
/// </summary>
public class OptionsArgumentType : ArgumentTypeBase
{
    private readonly string[] _options;

    public OptionsArgumentType(params string[] options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.Length == 0)
            throw new ArgumentException("At least one option must be provided", nameof(options));
    }

    public override string TypeName => "options";

    public override bool TryParse(string input, out object result)
    {
        result = null;

        if (string.IsNullOrEmpty(input))
            return false;

        var match = _options.FirstOrDefault(o =>
            o.Equals(input, StringComparison.OrdinalIgnoreCase)
        );

        if (match != null)
        {
            result = match;
            return true;
        }

        return false;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    )
    {
        if (string.IsNullOrEmpty(currentInput))
            return _options;

        return _options.Where(o => o.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase));
    }
}
