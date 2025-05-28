using System.Text.RegularExpressions;

namespace ITOC.Core.Command;

/// <summary>
/// Represents a node in the command tree that can have subcommands
/// </summary>
public class CommandNode : ICommand
{
    private static readonly Regex NameRegex = new(@"^[a-z0-9_\-.]+$", RegexOptions.Compiled);

    public string Name { get; }
    public string Description { get; }
    public IEnumerable<string> Aliases { get; }
    public string Permission { get; }

    // Add parent property
    public CommandNode Parent { get; private set; }

    private Func<CommandContext, Task<CommandResult>> _executor;
    private readonly Dictionary<string, CommandNode> _children = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CommandNode> _aliasMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CommandArgument> _arguments = new();

    public CommandNode(string name, string description = "", string permission = null, IEnumerable<string> aliases = null, Func<CommandContext, Task<CommandResult>> executor = null)
    {
        ValidateName(name);

        Name = name;
        Description = description ?? "";
        Permission = permission ?? "";
        Aliases = aliases?.ToList() ?? new List<string>();
        _executor = executor;
    }

    /// <summary>
    /// Gets all subcommands registered to this node
    /// </summary>
    public IEnumerable<CommandNode> Children => _children.Values;

    /// <summary>
    /// Gets all arguments registered to this command
    /// </summary>
    public IReadOnlyList<CommandArgument> Arguments => _arguments.AsReadOnly();

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (!NameRegex.IsMatch(name))
            throw new ArgumentException($"Invalid command name: '{name}'. Name can only contain letters, numbers, underscores, hyphens, and periods", nameof(name));
    }

    /// <summary>
    /// Adds a subcommand to this command
    /// </summary>
    public CommandNode Then(CommandNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_children.ContainsKey(child.Name))
            throw new ArgumentException($"A subcommand with the name '{child.Name}' already exists");

        _children[child.Name] = child;
        child.Parent = this; // Set the parent reference

        // Register all aliases
        foreach (var alias in child.Aliases)
        {
            if (_children.ContainsKey(alias) || _aliasMap.ContainsKey(alias))
                throw new ArgumentException($"A subcommand or alias with the name '{alias}' already exists");

            _aliasMap[alias] = child;
        }

        return this;
    }

    /// <summary>
    /// Adds an argument to this command
    /// </summary>
    public CommandNode WithArgument(CommandArgument argument)
    {
        _arguments.Add(argument ?? throw new ArgumentNullException(nameof(argument)));
        return this;
    }

    /// <summary>
    /// Gets a child command by name (or alias)
    /// </summary>
    public CommandNode GetChild(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (_children.TryGetValue(name, out var child))
            return child;

        if (_aliasMap.TryGetValue(name, out var aliasChild))
            return aliasChild;

        return null;
    }

    /// <summary>
    /// Executes this command with the given context
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        if (_executor == null)
            return CommandResult.Failure;

        if (!HasPermission(context.Sender))
            return CommandResult.PermissionDenied;

        return await _executor(context);
    }

    /// <summary>
    /// Sets the executor for this command
    /// </summary>
    /// <param name="executor">Function to execute when the command is run</param>
    public void SetExecutor(Func<CommandContext, Task<CommandResult>> executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Checks if the sender has permission to use this command
    /// </summary>
    public virtual bool HasPermission(object sender)
    {
        if (string.IsNullOrEmpty(Permission))
            return true;

        return sender is IPermissionHolder holder && holder.HasPermission(Permission);
    }
}
