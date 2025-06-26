using ITOC.Core.Command;

namespace ITOC.Test.Command;

public class CommandSystemExample
{
    private readonly CommandDispatcher _dispatcher;

    public CommandSystemExample()
    {
        _dispatcher = new CommandDispatcher();

        // Register example commands
        RegisterTeleportCommand();
        RegisterTeamCommands();
        RegisterGiveCommand();
        RegisterHelpCommand();
    }

    private void RegisterTeleportCommand()
    {
        var teleportCommand = CommandBuilder
            .Create("tp", "Teleport to coordinates or to another player", "command.teleport")
            .WithArgument("x", new FloatArgumentType(), "X coordinate")
            .WithArgument("y", new FloatArgumentType(), "Y coordinate")
            .WithArgument("z", new FloatArgumentType(), "Z coordinate")
            .Executes(context =>
            {
                var x = (float)context.Arguments["x"];
                var y = (float)context.Arguments["y"];
                var z = (float)context.Arguments["z"];

                Console.WriteLine($"Teleporting {context.Sender} to X:{x} Y:{y} Z:{z}");
                return CommandResult.Success;
            })
            .Build();

        _dispatcher.RegisterCommand(teleportCommand);
    }

    private void RegisterTeamCommands()
    {
        var teamCommand = CommandBuilder
            .Create("team", "Manage teams", "command.team")
            .Then("create", "Create a new team", "command.team.create")
            .WithArgument("name", new StringArgumentType(), "Team name")
            .WithOptionalArgument("color", new StringArgumentType(), "white", "Team color")
            .Executes(context =>
            {
                var name = (string)context.Arguments["name"];
                var color = (string)context.Arguments["color"];

                Console.WriteLine($"Created team '{name}' with color '{color}'");
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

                Console.WriteLine($"Added player '{player}' to team '{team}'");
                return CommandResult.Success;
            })
            .EndCommand()
            .Then("remove", "Remove a player from their team", "command.team.remove")
            .WithArgument("player", new StringArgumentType(), "Player name")
            .Executes(context =>
            {
                var player = (string)context.Arguments["player"];

                Console.WriteLine($"Removed player '{player}' from their team");
                return CommandResult.Success;
            })
            .EndCommand()
            .Then("list", "List all teams or members of a team", "command.team.list")
            .WithOptionalArgument("team", new StringArgumentType(), null, "Team name (optional)")
            .Executes(context =>
            {
                if (context.Arguments.TryGetValue("team", out var team) && team != null)
                    Console.WriteLine($"Listing members of team '{team}'");
                else
                    Console.WriteLine("Listing all teams");

                return CommandResult.Success;
            })
            .Build();

        _dispatcher.RegisterCommand(teamCommand);
    }

    private void RegisterGiveCommand()
    {
        var giveCommand = CommandBuilder
            .Create("give", "Give an item to a player", "command.give", "g")
            .WithArgument("player", new StringArgumentType(), "Player name")
            .WithArgument("item", new StringArgumentType(), "Item ID")
            .WithOptionalArgument(
                "amount",
                new IntegerArgumentType(1, 64),
                1,
                "Amount to give (1-64)"
            )
            .Executes(context =>
            {
                var player = (string)context.Arguments["player"];
                var item = (string)context.Arguments["item"];
                var amount = (int)context.Arguments["amount"];

                Console.WriteLine($"Giving {amount}x {item} to {player}");
                return CommandResult.Success;
            })
            .Build();

        _dispatcher.RegisterCommand(giveCommand);
    }

    private void RegisterHelpCommand()
    {
        var helpCommand = CommandBuilder
            .Create("help", "Get help with commands", "command.help", "?")
            .WithOptionalArgument(
                "command",
                new StringArgumentType(),
                null,
                "Command to get help for"
            )
            .Executes(context =>
            {
                if (context.Arguments.TryGetValue("command", out var cmdName) && cmdName != null)
                    Console.WriteLine($"Showing help for command: {cmdName}");
                else
                {
                    Console.WriteLine("Available commands:");
                    foreach (var cmd in _dispatcher.Commands)
                        Console.WriteLine($"  /{cmd.Name} - {cmd.Description}");
                }

                return CommandResult.Success;
            })
            .Build();

        _dispatcher.RegisterCommand(helpCommand);
    }

    public async Task<CommandResult> HandleCommandAsync(string commandInput, object sender) =>
        await _dispatcher.ExecuteAsync(commandInput, sender);

    public IEnumerable<string> GetSuggestions(string currentInput, object sender) =>
        _dispatcher.GetSuggestions(currentInput, sender);

    public static async Task RunConsoleExample()
    {
        var example = new CommandSystemExample();
        var player = new SimplePermissionHolder(
            "command.teleport",
            "command.team",
            "command.team.list",
            "command.help"
        );

        Console.WriteLine("Command System Example");
        Console.WriteLine("Type 'exit' to quit");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            var result = await example.HandleCommandAsync(input, player);

            switch (result)
            {
                case CommandResult.SyntaxError:
                    Console.WriteLine("Syntax error in command");
                    break;
                case CommandResult.PermissionDenied:
                    Console.WriteLine("You don't have permission to use that command");
                    break;
                case CommandResult.NotFound:
                    Console.WriteLine("Command not found");
                    break;
                case CommandResult.Failure:
                    Console.WriteLine("Command failed to execute");
                    break;
            }

            Console.WriteLine();
        }
    }
}
