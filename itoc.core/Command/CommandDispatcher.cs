using System.Text;

namespace ITOC.Core.Command;

/// <summary>
/// Result of a command parse operation
/// </summary>
public class ParseResult
{
    /// <summary>
    /// The command that was found (or null if not found)
    /// </summary>
    public CommandNode Command { get; }

    /// <summary>
    /// Whether the parse was successful
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Parsed arguments or null if not successful
    /// </summary>
    public Dictionary<string, object> Arguments { get; }

    /// <summary>
    /// Error message if not successful
    /// </summary>
    public string ErrorMessage { get; }

    private ParseResult(
        CommandNode command,
        bool success,
        Dictionary<string, object> arguments = null,
        string errorMessage = null
    )
    {
        Command = command;
        Success = success;
        Arguments = arguments;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful parse result
    /// </summary>
    public static ParseResult SuccessResult(
        CommandNode command,
        Dictionary<string, object> arguments
    ) => new ParseResult(command, true, arguments);

    public static ParseResult PermissionDeniedResult(CommandNode command) =>
        new ParseResult(
            command,
            false,
            errorMessage: "You do not have permission to execute this command"
        );

    /// <summary>
    /// Creates a failed parse result
    /// </summary>
    public static ParseResult FailureResult(CommandNode command, string errorMessage) =>
        new ParseResult(command, false, errorMessage: errorMessage);

    /// <summary>
    /// Creates a failed parse result for command not found
    /// </summary>
    public static ParseResult CommandNotFoundResult(string commandName) =>
        new ParseResult(null, false, errorMessage: $"Unknown command: {commandName}");
}

/// <summary>
/// Command parser and dispatcher
/// </summary>
public class CommandDispatcher
{
    private readonly Dictionary<string, CommandNode> _rootCommands = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, CommandNode> _commandAliases = new(
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>
    /// Prefix used for commands (e.g., "/" or "!")
    /// </summary>
    public string CommandPrefix { get; set; } = "/";

    /// <summary>
    /// Gets all registered root commands
    /// </summary>
    public IEnumerable<CommandNode> Commands => _rootCommands.Values;

    /// <summary>
    /// Registers a command with the dispatcher
    /// </summary>
    /// <param name="command">The command to register</param>
    public void RegisterCommand(CommandNode command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_rootCommands.ContainsKey(command.Name))
            throw new ArgumentException(
                $"A command with the name '{command.Name}' is already registered"
            );

        _rootCommands.Add(command.Name, command);

        // Register aliases
        foreach (var alias in command.Aliases)
        {
            if (_rootCommands.ContainsKey(alias) || _commandAliases.ContainsKey(alias))
                throw new ArgumentException(
                    $"A command or alias with the name '{alias}' is already registered"
                );

            _commandAliases.Add(alias, command);
        }
    }

    /// <summary>
    /// Unregisters a command from the dispatcher
    /// </summary>
    /// <param name="commandName">The name of the command to unregister</param>
    /// <returns>True if the command was unregistered, false if it wasn't found</returns>
    public bool UnregisterCommand(string commandName)
    {
        if (string.IsNullOrEmpty(commandName))
            return false;

        if (!_rootCommands.TryGetValue(commandName, out var command))
            return false;

        _rootCommands.Remove(commandName);

        // Clean up aliases
        var aliasesToRemove = command
            .Aliases.Where(a => _commandAliases.ContainsKey(a) && _commandAliases[a] == command)
            .ToList();

        foreach (var alias in aliasesToRemove)
            _commandAliases.Remove(alias);

        return true;
    }

    /// <summary>
    /// Checks if the input string starts with the command prefix
    /// </summary>
    public bool HasCommandPrefix(string input)
    {
        if (string.IsNullOrEmpty(CommandPrefix))
            return false;

        return input.StartsWith(CommandPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes the command prefix (and leading spaces) from the input string if it exists
    /// </summary>
    public string RemoveCommandPrefix(string input)
    {
        if (string.IsNullOrEmpty(CommandPrefix) || !input.StartsWith(CommandPrefix))
            return input;

        return input[CommandPrefix.Length..].TrimStart();
    }

    /// <summary>
    /// Parses a command string into a command and arguments
    /// </summary>
    /// <param name="input">The command string to parse</param>
    /// <returns>The parse result</returns>
    public ParseResult Parse(string input, object sender = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ParseResult.FailureResult(null, "No command provided");

        input = RemoveCommandPrefix(input);

        var args = SplitArguments(input);

        if (args.Length == 0)
            return ParseResult.FailureResult(null, "No command provided");

        var commandName = args[0];

        // Find the command
        if (
            !_rootCommands.TryGetValue(commandName, out var command)
            && !_commandAliases.TryGetValue(commandName, out command)
        )
            return ParseResult.CommandNotFoundResult(commandName);

        // Navigate the command tree
        var argIndex = 1;
        while (argIndex < args.Length)
        {
            var next = command.GetChild(args[argIndex]);
            if (next == null)
                break;

            command = next;
            argIndex++;
        }

        // Check permissions
        if (!command.HasPermission(sender))
            return ParseResult.PermissionDeniedResult(command);

        // Parse the arguments
        var parsedArgs = new Dictionary<string, object>();
        var requiredArgs = command.Arguments.Where(a => a.IsRequired).ToList();
        var optionalArgs = command.Arguments.Where(a => !a.IsRequired).ToList();

        // Check if we have enough arguments for all required parameters
        if (args.Length - argIndex < requiredArgs.Count)
            return ParseResult.FailureResult(
                command,
                $"Not enough arguments. Required: {string.Join(", ", requiredArgs.Select(a => a.Name))}"
            );

        // Parse required arguments
        for (var i = 0; i < requiredArgs.Count; i++)
        {
            var arg = requiredArgs[i];
            var value = args[argIndex + i];

            if (!arg.Type.TryParse(value, out var parsedValue))
                return ParseResult.FailureResult(
                    command,
                    $"Invalid value for argument '{arg.Name}': {value}"
                );

            parsedArgs.Add(arg.Name, parsedValue);
        }

        // Parse optional arguments if provided
        var optionalArgsStart = argIndex + requiredArgs.Count;
        for (var i = 0; i < optionalArgs.Count && optionalArgsStart + i < args.Length; i++)
        {
            var arg = optionalArgs[i];
            var value = args[optionalArgsStart + i];

            if (arg.Type.TryParse(value, out var parsedValue))
                parsedArgs.Add(arg.Name, parsedValue);
            else
                parsedArgs.Add(arg.Name, arg.DefaultValue);
        }

        // Add default values for any remaining optional arguments
        foreach (var arg in optionalArgs.Where(a => !parsedArgs.ContainsKey(a.Name)))
            parsedArgs.Add(arg.Name, arg.DefaultValue);

        return ParseResult.SuccessResult(command, parsedArgs);
    }

    /// <summary>
    /// Executes a command from an input string
    /// </summary>
    /// <param name="input">The command string to execute</param>
    /// <param name="sender">The sender of the command</param>
    /// <returns>The result of the command execution</returns>
    public async Task<CommandResult> ExecuteAsync(string input, object sender)
    {
        var parseResult = Parse(input, sender);

        if (!parseResult.Success)
        {
            if (!parseResult.Command.HasPermission(sender))
                return CommandResult.PermissionDenied;

            return CommandResult.SyntaxError;
        }

        if (parseResult.Command == null)
            return CommandResult.NotFound;

        var context = new CommandContext(sender, input, parseResult.Arguments);
        return await parseResult.Command.ExecuteAsync(context);
    }

    /// <summary>
    /// Gets all root command names
    /// </summary>
    public IEnumerable<string> AllRootCommandNames() =>
        _rootCommands.Keys.Concat(_commandAliases.Keys);

    /// <summary>
    /// Gets all root command names that the sender has permission for
    /// </summary>
    /// <param name="sender">The sender requesting the command names</param>
    public IEnumerable<string> AllRootCommandNames(object sender)
    {
        var commands = _rootCommands
            .Values.Where(c => c.HasPermission(sender))
            .Select(c => $"{CommandPrefix}{c.Name}");

        var aliases = _commandAliases
            .Where(pair => pair.Value.HasPermission(sender))
            .Select(pair => $"{CommandPrefix}{pair.Key}");

        return commands.Concat(aliases);
    }

    /// <summary>
    /// Gets suggestions for the current input
    /// </summary>
    /// <param name="input">The current input string</param>
    /// <param name="sender">The sender requesting suggestions</param>
    /// <returns>A list of suggestion strings</returns>
    public IEnumerable<string> GetSuggestions(string input, object sender)
    {
        if (string.IsNullOrEmpty(input))
            return AllRootCommandNames(sender);

        input = RemoveCommandPrefix(input);

        var args = SplitArguments(input);
        var inputEndWithSpace = input.EndsWith(' ');

        // If no arguments or just the command prefix, suggest all commands
        if (args.Length == 0 || (args.Length == 1 && !inputEndWithSpace))
        {
            // Suggesting commands
            if (args.Length == 0)
                return AllRootCommandNames(sender);

            var commands = _rootCommands
                .Values.Where(c =>
                    c.HasPermission(sender)
                    && c.Name.StartsWith(args[0], StringComparison.OrdinalIgnoreCase)
                )
                .Select(c => $"{CommandPrefix}{c.Name}");

            var aliases = _commandAliases
                .Where(pair =>
                    pair.Value.HasPermission(sender)
                    && pair.Key.StartsWith(args[0], StringComparison.OrdinalIgnoreCase)
                )
                .Select(pair => $"{CommandPrefix}{pair.Key}");

            return commands.Concat(aliases);
        }

        // Find the command
        var commandName = args[0];

        if (
            !_rootCommands.TryGetValue(commandName, out var command)
            && !_commandAliases.TryGetValue(commandName, out command)
        )
            return [];

        // Navigate the command tree
        var argIndex = 1;
        while (argIndex < args.Length)
        {
            var subCommand = command.GetChild(args[argIndex]);
            if (subCommand == null)
                break;

            command = subCommand;
            argIndex++;
        }

        if (command.HasPermission(sender) == false)
            return [];

        if (args.Length == argIndex && !inputEndWithSpace)
            return [];

        var prefix = inputEndWithSpace ? string.Empty : args[^1];

        // Suggest subcommands
        var subCommands = command
            .Children.Where(c =>
                c.HasPermission(sender)
                && c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            )
            .Select(c => c.Name);

        // Suggest arguments by order
        var commandArgIndex = args.Length - argIndex - (inputEndWithSpace ? 0 : 1);
        if (command.Arguments.Count <= commandArgIndex)
            return subCommands;

        var commandArg = command.Arguments[commandArgIndex];
        var suggestions = commandArg.GetSuggestions(prefix, input);
        return subCommands.Concat(suggestions);
    }

    /// <summary>
    /// Splits a command string into arguments, respecting quotes
    /// </summary>
    public static string[] SplitArguments(string input)
    {
        // TODO: Initial spaces

        var args = new List<string>();
        var currentArg = new StringBuilder();
        var inQuote = false;
        var quoteChar = '"';

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if ((c == '"' || c == '\'') && (i == 0 || input[i - 1] != '\\'))
            {
                if (inQuote && c == quoteChar)
                {
                    // End of quoted section
                    inQuote = false;
                }
                else if (!inQuote)
                {
                    // Start of quoted section
                    inQuote = true;
                    quoteChar = c;
                }
                else
                {
                    // Different quote char inside a quote, treat as literal
                    currentArg.Append(c);
                }
            }
            else if (char.IsWhiteSpace(c) && !inQuote)
            {
                // End of argument
                if (currentArg.Length > 0)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                }
            }
            else if (
                c == '\\'
                && i < input.Length - 1
                && (input[i + 1] == '"' || input[i + 1] == '\'')
            )
            {
                // Escape sequence for quotes
                continue;
            }
            else
            {
                currentArg.Append(c);
            }
        }

        if (currentArg.Length > 0)
            args.Add(currentArg.ToString());

        return args.ToArray();
    }
}
