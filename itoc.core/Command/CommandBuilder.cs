namespace ITOC.Core.Command;

/// <summary>
/// A fluent builder for creating command trees
/// </summary>
public class CommandBuilder
{
    private CommandNode _currentNode;

    private CommandBuilder(CommandNode node) =>
        _currentNode = node ?? throw new ArgumentNullException(nameof(node));

    /// <summary>
    /// Starts building a new command
    /// </summary>
    /// <param name="name">The command name</param>
    /// <param name="description">Command description</param>
    /// <param name="permission">Permission required to use the command</param>
    /// <param name="aliases">Alternative names for the command</param>
    /// <returns>A new command builder</returns>
    public static CommandBuilder Create(
        string name,
        string description = "",
        string permission = null,
        params string[] aliases
    ) => new CommandBuilder(new CommandNode(name, description, permission, aliases));

    /// <summary>
    /// Adds a subcommand to the current command
    /// </summary>
    /// <param name="name">The subcommand name</param>
    /// <param name="description">Subcommand description</param>
    /// <param name="permission">Permission required to use the subcommand</param>
    /// <param name="aliases">Alternative names for the subcommand</param>
    /// <returns>A builder for the subcommand</returns>
    public CommandBuilder Then(
        string name,
        string description = "",
        string permission = null,
        params string[] aliases
    )
    {
        var subCommand = new CommandNode(name, description, permission, aliases);
        _currentNode.Then(subCommand);
        return new CommandBuilder(subCommand);
    }

    /// <summary>
    /// Adds a required argument to the current command
    /// </summary>
    /// <param name="name">Name of the argument</param>
    /// <param name="type">Type of the argument</param>
    /// <param name="description">Description of the argument</param>
    /// <param name="suggestionsOverride">Optional override for suggestions</param>
    /// <returns>This builder</returns>
    public CommandBuilder WithArgument(
        string name,
        IArgumentType type,
        string description = "",
        string[] suggestionsOverride = null
    )
    {
        if (_currentNode.Arguments.Count != 0 && !_currentNode.Arguments[^1].IsRequired)
            throw new InvalidOperationException(
                "Cannot add a required argument after an optional one. Use WithOptionalArgument for optional arguments."
            );

        _currentNode.WithArgument(
            new CommandArgument(name, type, description, suggestionsOverride, true)
        );
        return this;
    }

    /// <summary>
    /// Adds an optional argument with a default value to the current command
    /// </summary>
    /// <param name="name">Name of the argument</param>
    /// <param name="type">Type of the argument</param>
    /// <param name="defaultValue">Default value if not provided</param>
    /// <param name="description">Description of the argument</param>
    /// <param name="suggestionsOverride">Optional override for suggestions</param>
    /// <returns>This builder</returns>
    public CommandBuilder WithOptionalArgument(
        string name,
        IArgumentType type,
        object defaultValue,
        string description = "",
        string[] suggestionsOverride = null
    )
    {
        _currentNode.WithArgument(
            new CommandArgument(name, type, description, suggestionsOverride, false, defaultValue)
        );
        return this;
    }

    /// <summary>
    /// Sets the executor for the current command
    /// </summary>
    /// <param name="executor">Function to execute when the command is run</param>
    /// <returns>This builder</returns>
    public CommandBuilder Executes(Func<CommandContext, Task<CommandResult>> executor)
    {
        _currentNode.SetExecutor(executor);
        return this;
    }

    /// <summary>
    /// Sets the executor for the current command with a synchronous function
    /// </summary>
    /// <param name="executor">Function to execute when the command is run</param>
    /// <returns>This builder</returns>
    public CommandBuilder Executes(Func<CommandContext, CommandResult> executor) =>
        Executes(ctx => Task.FromResult(executor(ctx)));

    /// <summary>
    /// Returns to the parent command builder
    /// </summary>
    /// <returns>The parent command builder</returns>
    public CommandBuilder EndCommand() =>
        _currentNode.Parent == null
            ? throw new InvalidOperationException(
                "This is a root command, there is no parent to return to"
            )
            : new CommandBuilder(_currentNode.Parent);

    /// <summary>
    /// Builds and returns the command node
    /// </summary>
    /// <returns>The built command node</returns>
    public CommandNode Build()
    {
        // if (_currentNode.Parent != null)
        // throw new InvalidOperationException("Cannot build a command that is not a root command. Use EndCommand() to return to the root.");

        // Traverse up to the root node
        while (_currentNode.Parent != null)
            _currentNode = _currentNode.Parent;

        return _currentNode;
    }
}
