using System;
using System.Text.RegularExpressions;

namespace ITOC;

/// <summary>
/// Format: "namespace:path" (e.g., "itoc:stone")
/// </summary>
public readonly struct Identifier : IEquatable<Identifier>, IComparable<Identifier>
{
    private static readonly Regex NamespaceRegex = new(@"^[a-zA-Z0-9_\-.]+$", RegexOptions.Compiled);
    private static readonly Regex PathRegex = new(@"^[a-zA-Z0-9_\-.]+$", RegexOptions.Compiled);

    public const string ItocNamespace = "itoc";
    public const string NamespaceSeparator = ":";

    public string Namespace { get; }
    public string Path { get; }

    /// <summary>
    /// Creates a new identifier with the specified namespace and path
    /// </summary>
    public Identifier(string @namespace, string path)
    {
        ValidateNamespace(@namespace);
        ValidatePath(path);

        Namespace = @namespace;
        Path = path;
    }

    /// <summary>
    /// Creates a new identifier from a string in the format "namespace:path"
    /// If no namespace is specified, the default namespace is used
    /// </summary>
    public Identifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

        string @namespace;
        string path;

        if (identifier.Contains(NamespaceSeparator))
        {
            var parts = identifier.Split(NamespaceSeparator, 2);
            @namespace = parts[0];
            path = parts[1];
        }
        else
        {
            // Namespace must be specified explicitly.
            throw new ArgumentException($"Invalid identifier format: '{identifier}'. Expected format is 'namespace:path'", nameof(identifier));
        }

        ValidateNamespace(@namespace);
        ValidatePath(path);

        Namespace = @namespace;
        Path = path;
    }

    private static void ValidateNamespace(string @namespace)
    {
        if (string.IsNullOrEmpty(@namespace))
            throw new ArgumentException("Namespace cannot be null or empty", nameof(@namespace));

        if (!NamespaceRegex.IsMatch(@namespace))
            throw new ArgumentException($"Invalid namespace format: '{@namespace}'. Namespace can only contain lowercase letters, numbers, underscores, hyphens, and periods", nameof(@namespace));
    }

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!PathRegex.IsMatch(path))
            throw new ArgumentException($"Invalid path format: '{path}'. Path can only contain lowercase letters, numbers, underscores, hyphens, periods, and forward slashes", nameof(path));
    }

    /// <summary>
    /// Returns the string representation of this identifier (namespace:path)
    /// </summary>
    public override string ToString() => $"{Namespace}{NamespaceSeparator}{Path}";

    public bool Equals(Identifier other) =>
        string.Equals(Namespace, other.Namespace) &&
        string.Equals(Path, other.Path);

    public override bool Equals(object obj) =>
        obj is Identifier other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Namespace, Path);

    public int CompareTo(Identifier other)
    {
        var namespaceComparison = string.Compare(Namespace, other.Namespace, StringComparison.Ordinal);
        return namespaceComparison != 0 ? namespaceComparison : string.Compare(Path, other.Path, StringComparison.Ordinal);
    }

    public static bool operator ==(Identifier left, Identifier right) => left.Equals(right);
    public static bool operator !=(Identifier left, Identifier right) => !left.Equals(right);
    public static bool operator <(Identifier left, Identifier right) => left.CompareTo(right) < 0;
    public static bool operator >(Identifier left, Identifier right) => left.CompareTo(right) > 0;
    public static bool operator <=(Identifier left, Identifier right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Identifier left, Identifier right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Implicitly converts a string to an Identifier
    /// </summary>
    public static implicit operator Identifier(string identifier) => new(identifier);
}
