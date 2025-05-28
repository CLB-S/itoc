namespace ITOC.Core.Command;

/// <summary>
/// Represents the result of a command execution
/// </summary>
public enum CommandResult
{
    Success,
    Failure,
    SyntaxError,
    PermissionDenied,
    NotFound
}

/// <summary>
/// Context passed to commands when being executed
/// </summary>
public class CommandContext
{
    /// <summary>
    /// The sender of the command
    /// </summary>
    public object Sender { get; }

    /// <summary>
    /// Raw input string used for the command
    /// </summary>
    public string RawInput { get; }

    /// <summary>
    /// Parsed arguments for the command
    /// </summary>
    public Dictionary<string, object> Arguments { get; }

    public CommandContext(object sender, string rawInput, Dictionary<string, object> arguments)
    {
        Sender = sender;
        RawInput = rawInput;
        Arguments = arguments ?? new Dictionary<string, object>();
    }
}

/// <summary>
/// Interface for command objects in the command system
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Primary name of the command
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what the command does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Alternative names that can be used to execute this command
    /// </summary>
    IEnumerable<string> Aliases { get; }

    /// <summary>
    /// Permission required to use this command
    /// </summary>
    string Permission { get; }

    /// <summary>
    /// Executes the command with the given context
    /// </summary>
    /// <param name="context">The context for command execution</param>
    /// <returns>A task that resolves to the result of the command</returns>
    Task<CommandResult> ExecuteAsync(CommandContext context);

    /// <summary>
    /// Checks if the sender has permission to use this command
    /// </summary>
    /// <param name="sender">The sender trying to use the command</param>
    /// <returns>True if the sender has permission, false otherwise</returns>
    bool HasPermission(object sender);
}
