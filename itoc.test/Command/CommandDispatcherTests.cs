using ITOC.Core.Command;

namespace ITOC.Test.Command;

public class CommandDispatcherTests
{
    [Fact]
    public void RegisterCommand_AddsCommand()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();
        var command = new CommandNode("test");

        // Act
        dispatcher.RegisterCommand(command);

        // Assert
        Assert.Single(dispatcher.Commands);
        Assert.Contains(dispatcher.Commands, c => c.Name == "test");
    }

    [Fact]
    public void RegisterCommand_ThrowsOnDuplicate()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();
        var command1 = new CommandNode("test");
        var command2 = new CommandNode("test");

        // Act
        dispatcher.RegisterCommand(command1);

        // Assert
        Assert.Throws<ArgumentException>(() => dispatcher.RegisterCommand(command2));
    }

    [Fact]
    public void UnregisterCommand_RemovesCommand()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();
        var command = new CommandNode("test");
        dispatcher.RegisterCommand(command);

        // Act
        var result = dispatcher.UnregisterCommand("test");

        // Assert
        Assert.True(result);
        Assert.Empty(dispatcher.Commands);
    }

    [Fact]
    public void Parse_HandlesEmptyInput()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();

        // Act
        var result = dispatcher.Parse("");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No command provided", result.ErrorMessage);
    }

    [Fact]
    public void Parse_StripsPrefix()
    {
        // Arrange
        var dispatcher = new CommandDispatcher { CommandPrefix = "/" };
        var command = CommandBuilder.Create("test").Build();
        dispatcher.RegisterCommand(command);

        // Act
        var result = dispatcher.Parse("/test");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(command, result.Command);
    }

    [Fact]
    public void Parse_HandlesUnknownCommand()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();

        // Act
        var result = dispatcher.Parse("unknown");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unknown command", result.ErrorMessage);
    }

    [Fact]
    public void Parse_NavigatesCommandTree()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();
        var command = CommandBuilder.Create("parent")
            .Then("child")
            .Build();
        dispatcher.RegisterCommand(command);

        // Act
        var result = dispatcher.Parse("parent child");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(command.GetChild("child"), result.Command);
    }

    [Fact]
    public void Parse_ParsesArguments()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();
        var command = CommandBuilder.Create("test")
            .WithArgument("arg1", new StringArgumentType())
            .WithArgument("arg2", new IntegerArgumentType())
            .Build();
        dispatcher.RegisterCommand(command);

        // Act
        var result = dispatcher.Parse("test hello 123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("hello", result.Arguments["arg1"]);
        Assert.Equal(123, result.Arguments["arg2"]);
    }

    [Fact]
    public void Parse_ChecksRequiredArguments()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();
        var command = CommandBuilder.Create("test")
            .WithArgument("required", new StringArgumentType())
            .Build();
        dispatcher.RegisterCommand(command);

        // Act
        var result = dispatcher.Parse("test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Not enough arguments", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesCommand()
    {
        // Arrange
        bool executed = false;
        var dispatcher = new CommandDispatcher();
        var command = CommandBuilder.Create("test")
            .Executes(_ =>
            {
                executed = true;
                return Task.FromResult(CommandResult.Success);
            })
            .Build();
        dispatcher.RegisterCommand(command);

        // Act
        var result = await dispatcher.ExecuteAsync("test", new TestPermissionHolder());

        // Assert
        Assert.Equal(CommandResult.Success, result);
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesPermissions()
    {
        // Arrange
        var dispatcher = new CommandDispatcher();
        var command = CommandBuilder.Create("test", permission: "test.permission")
            .Executes(_ => Task.FromResult(CommandResult.Success))
            .Build();
        dispatcher.RegisterCommand(command);

        // Act
        var resultNoPermission = await dispatcher.ExecuteAsync("test", new TestPermissionHolder());
        var resultWithPermission = await dispatcher.ExecuteAsync("test", new TestPermissionHolder("test.permission"));

        // Assert
        Assert.Equal(CommandResult.PermissionDenied, resultNoPermission);
        Assert.Equal(CommandResult.Success, resultWithPermission);
    }

    [Fact]
    public void GetSuggestions_ReturnsAvailableCommands()
    {
        // Arrange
        var dispatcher = new CommandDispatcher { CommandPrefix = "/" };
        dispatcher.RegisterCommand(CommandBuilder.Create("test1").Build());
        dispatcher.RegisterCommand(CommandBuilder.Create("test2").Build());

        // Act
        var suggestions = dispatcher.GetSuggestions("", new TestPermissionHolder()).ToList();

        // Assert
        Assert.Equal(2, suggestions.Count);
        Assert.Contains("/test1", suggestions);
        Assert.Contains("/test2", suggestions);
    }

    [Fact]
    public void GetSuggestions_FiltersBasedOnPrefix()
    {
        // Arrange
        var dispatcher = new CommandDispatcher { CommandPrefix = "/" };
        dispatcher.RegisterCommand(CommandBuilder.Create("test1").Build());
        dispatcher.RegisterCommand(CommandBuilder.Create("test2").Build());
        dispatcher.RegisterCommand(CommandBuilder.Create("other").Build());

        // Act
        var suggestions = dispatcher.GetSuggestions("/t", new TestPermissionHolder()).ToList();

        // Assert
        Assert.Equal(2, suggestions.Count);
        Assert.Contains("/test1", suggestions);
        Assert.Contains("/test2", suggestions);
    }

    [Fact]
    public void SplitArguments_HandlesSimpleArguments()
    {
        // Arrange & Act
        var result = CommandDispatcher.SplitArguments("command arg1 arg2 arg3");

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal("command", result[0]);
        Assert.Equal("arg1", result[1]);
        Assert.Equal("arg2", result[2]);
        Assert.Equal("arg3", result[3]);
    }

    [Fact]
    public void SplitArguments_HandlesDoubleQuotedArguments()
    {
        // Arrange & Act
        var result = CommandDispatcher.SplitArguments("command \"quoted arg\" arg2");

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("command", result[0]);
        Assert.Equal("quoted arg", result[1]);
        Assert.Equal("arg2", result[2]);
    }

    [Fact]
    public void SplitArguments_HandlesSingleQuotedArguments()
    {
        // Arrange & Act
        var result = CommandDispatcher.SplitArguments("command 'quoted arg' arg2");

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("command", result[0]);
        Assert.Equal("quoted arg", result[1]);
        Assert.Equal("arg2", result[2]);
    }

    [Fact]
    public void SplitArguments_HandlesEscapedQuotes()
    {
        // Arrange & Act
        var result = CommandDispatcher.SplitArguments("command arg\\\"withquote arg2");

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("command", result[0]);
        Assert.Equal("arg\"withquote", result[1]);
        Assert.Equal("arg2", result[2]);
    }

    [Fact]
    public void SplitArguments_HandlesMixedQuoteTypes()
    {
        // Arrange & Act
        var result = CommandDispatcher.SplitArguments("command \"outer 'inner' quotes\" 'outer \"inner\" quotes'");

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("command", result[0]);
        Assert.Equal("outer 'inner' quotes", result[1]);
        Assert.Equal("outer \"inner\" quotes", result[2]);
    }

    [Fact]
    public void SplitArguments_HandlesEmptyInput()
    {
        // Arrange & Act
        var result = CommandDispatcher.SplitArguments("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SplitArguments_HandlesConsecutiveSpaces()
    {
        // Arrange & Act
        var result = CommandDispatcher.SplitArguments("command  multiple   spaces     ");

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("command", result[0]);
        Assert.Equal("multiple", result[1]);
        Assert.Equal("spaces", result[2]);
    }
}
