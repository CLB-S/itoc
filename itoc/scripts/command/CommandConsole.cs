// For test.

using Godot;
using ITOC.Core.Command;

namespace ITOC;

/// <summary>
/// Example of integrating the command system with Godot
/// </summary>
public partial class CommandConsole : Node
{
    private CommandDispatcher _commandDispatcher;
    private RichTextLabel _outputLabel;
    private LineEdit _inputLineEdit;
    private ItemList _suggestionsList;

    private SimplePermissionHolder _player;

    public override void _Ready()
    {
        _commandDispatcher = new CommandDispatcher();

        // Setup UI references
        _outputLabel = GetNode<RichTextLabel>("OutputRichTextLabel");
        _inputLineEdit = GetNode<LineEdit>("InputLineEdit");
        _suggestionsList = GetNode<ItemList>("InputLineEdit/SuggestionsList");

        // Clear
        _inputLineEdit.Text = "";
        _suggestionsList.Clear();
        _suggestionsList.Visible = false;

        // Create a player with permissions
        _player = new SimplePermissionHolder(
            "command.teleport",
            "command.time",
            "command.give",
            "command.team",
            "command.team.create",
            "command.team.add",
            // "command.team.remove",
            "command.team.list",
            "command.help"
        );

        // Register commands
        RegisterCommands();

        // Connect signals
        _inputLineEdit.TextChanged += OnInputTextChanged;
        _inputLineEdit.TextSubmitted += OnInputSubmitted;
        _suggestionsList.ItemSelected += OnSuggestionSelected;
    }

    private void RegisterCommands()
    {
        // Teleport command
        var teleportCommand = CommandBuilder.Create("tp", "Teleport to coordinates or to another player", "command.teleport")
            .WithArgument("x", new FloatArgumentType(), "X coordinate")
            .WithArgument("y", new FloatArgumentType(), "Y coordinate")
            .WithArgument("z", new FloatArgumentType(), "Z coordinate")
            .Executes(context =>
            {
                var x = (float)context.Arguments["x"];
                var y = (float)context.Arguments["y"];
                var z = (float)context.Arguments["z"];

                LogOutput($"Teleporting player to X:{x} Y:{y} Z:{z}");

                // In a real implementation, we would teleport the player here
                // e.g. player.GlobalPosition = new Vector3(x, y, z);

                return CommandResult.Success;
            })
            .Build();

        _commandDispatcher.RegisterCommand(teleportCommand);

        // Give command with custom item type
        var giveCommand = CommandBuilder.Create("give", "Give an item to a player", "command.give")
            .WithArgument("player", new PlayerArgumentType(), "Player name")
            .WithArgument("item", new ItemArgumentType(), "Item to give")
            .WithOptionalArgument("amount", new IntegerArgumentType(1, 100), 1, "Amount to give (1-100)")
            .Executes(context =>
            {
                var player = (string)context.Arguments["player"];
                var item = (string)context.Arguments["item"];
                var amount = (int)context.Arguments["amount"];

                LogOutput($"Giving {amount}x {item} to {player}");

                // In a real implementation, we would give the item to the player here

                return CommandResult.Success;
            })
            .Build();

        _commandDispatcher.RegisterCommand(giveCommand);

        // Time command with enum type
        var timeCommand = CommandBuilder.Create("time", "Change the game time", "command.time")
            .WithArgument("value", new EnumArgumentType<TimeOfDay>(), "Time of day")
            .Executes(context =>
            {
                var time = (TimeOfDay)context.Arguments["value"];

                LogOutput($"Setting time to {time}");

                // In a real implementation, we would change the time here

                return CommandResult.Success;
            })
            .Build();

        _commandDispatcher.RegisterCommand(timeCommand);

        // Team command
        var teamCommand = CommandBuilder.Create("team", "Manage teams", "command.team")
            .Then("create", "Create a new team", "command.team.create")
                .WithArgument("name", new StringArgumentType(), "Team name")
                .WithOptionalArgument("color", new StringArgumentType(), "white", "Team color")
                .Executes(context =>
                {
                    var name = (string)context.Arguments["name"];
                    var color = (string)context.Arguments["color"];

                    LogOutput($"Created team '{name}' with color '{color}'");
                    return CommandResult.Success;
                })
            .EndCommand()
            .Then("add", "Add a player to a team", "command.team.add")
                .WithArgument("team", new StringArgumentType(), "Team name")
                .WithArgument("player", new StringArgumentType(), "Player name")
                .Executes(context =>
                {
                    var team = (string)context.Arguments["team"];
                    var player = (string)context.Arguments["player"];

                    LogOutput($"Added player '{player}' to team '{team}'");
                    return CommandResult.Success;
                })
            .EndCommand()
            .Then("remove", "Remove a player from their team", "command.team.remove")
                .WithArgument("player", new StringArgumentType(), "Player name")
                .Executes(context =>
                {
                    var player = (string)context.Arguments["player"];

                    LogOutput($"Removed player '{player}' from their team");
                    return CommandResult.Success;
                })
            .EndCommand()
            .Then("list", "List all teams or members of a team", "command.team.list")
                .WithOptionalArgument("team", new StringArgumentType(), null, "Team name (optional)")
                .Executes(context =>
                {
                    if (context.Arguments.TryGetValue("team", out var team) && team != null)
                        LogOutput($"Listing members of team '{team}'");
                    else
                        LogOutput("Listing all teams");

                    return CommandResult.Success;
                })
            .Build();

        _commandDispatcher.RegisterCommand(teamCommand);

        // Help command
        var helpCommand = CommandBuilder.Create("help", "Show available commands", "command.help", "?")
            .WithOptionalArgument("command", new StringArgumentType(), null, "Command to get help for")
            .Executes(context =>
            {
                if (context.Arguments.TryGetValue("command", out var cmdName) && cmdName != null)
                {
                    // Show help for specific command
                    LogOutput($"Help for command: {cmdName}");
                    // Detailed implementation would look up command info
                }
                else
                {
                    // List all available commands
                    LogOutput("Available commands:");
                    foreach (var cmd in _commandDispatcher.Commands)
                    {
                        if (cmd.HasPermission(_player))
                            LogOutput($"/{cmd.Name} - {cmd.Description}");
                    }
                }

                return CommandResult.Success;
            })
            .Build();

        _commandDispatcher.RegisterCommand(helpCommand);
    }

    private void OnInputTextChanged(string text)
    {
        UpdateSuggestions(text);
    }

    private async void OnInputSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        LogOutput($"> {text}");

        _inputLineEdit.Text = "";
        ClearSuggestions();

        if (!_commandDispatcher.HasCommandPrefix(text))
            return;

        // Execute command
        var result = await _commandDispatcher.ExecuteAsync(text, _player);

        // Handle command result
        switch (result)
        {
            case CommandResult.SyntaxError:
                LogOutput("Syntax error. Type /help for available commands.");
                break;
            case CommandResult.PermissionDenied:
                LogOutput("You don't have permission to use that command.");
                break;
            case CommandResult.NotFound:
                LogOutput("Unknown command. Type /help for available commands.");
                break;
            case CommandResult.Failure:
                LogOutput("Command failed to execute.");
                break;
        }
    }

    private void OnSuggestionSelected(long index)
    {
        var suggestion = _suggestionsList.GetItemText((int)index);
        _inputLineEdit.Text = suggestion;
        _inputLineEdit.GrabFocus();
        _inputLineEdit.CaretColumn = suggestion.Length;

        ClearSuggestions();
    }

    private void UpdateSuggestions(string text)
    {
        // Clear previous suggestions
        ClearSuggestions();

        if (string.IsNullOrEmpty(text))
            return;

        // Get suggestions from command system
        var suggestions = _commandDispatcher.GetSuggestions(text, _player);

        // GD.Print($"Suggestions for '{text}': {string.Join(", ", suggestions)}");

        // Add suggestions to list
        foreach (var suggestion in suggestions) // .Take(10)) // Limit to 10 suggestions
            _suggestionsList.AddItem(suggestion);

        // Show suggestions if there are any
        _suggestionsList.Visible = _suggestionsList.ItemCount > 0;

        CallDeferred(MethodName.RelocateSuggestionsList, text);
    }

    private void RelocateSuggestionsList(string text)
    {
        // Reset size to zero to get auto-sizing right. 
        _suggestionsList.Size = Vector2.Zero;

        var font = _inputLineEdit.GetThemeDefaultFont();
        var fontSize = _inputLineEdit.GetThemeDefaultFontSize();
        var textSize = font.GetStringSize(text, fontSize: fontSize);
        var listSize = _suggestionsList.Size;
        _suggestionsList.Position = new Vector2(textSize.X, -listSize.Y);
    }

    private void ClearSuggestions()
    {
        _suggestionsList.Visible = false;
        _suggestionsList.Clear();
    }

    private void LogOutput(string message)
    {
        GD.Print(message);

        // Add to UI output
        _outputLabel.Text += $"{message}\n";
    }

    // Example enum for command arguments
    public enum TimeOfDay
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Dusk,
        Night,
        Midnight
    }
}
