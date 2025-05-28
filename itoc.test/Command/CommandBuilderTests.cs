using ITOC.Core.Command;

namespace ITOC.Test.Command;

public class CommandBuilderTests
{
    [Fact]
    public void Create_BuildsCommand_Correctly()
    {
        // Act
        var command = CommandBuilder.Create("test", "Test command", "test.permission", "t")
            .Build();

        // Assert
        Assert.Equal("test", command.Name);
        Assert.Equal("Test command", command.Description);
        Assert.Equal("test.permission", command.Permission);
        Assert.Contains("t", command.Aliases);
    }

    [Fact]
    public void Then_BuildsSubcommands_Correctly()
    {
        // Act
        var command = CommandBuilder.Create("parent")
            .Then("child", "Child command")
                .Then("grandchild", "Grandchild command")
                .EndCommand()
            .EndCommand()
            .Then("child2")
            .Build();

        // Assert
        Assert.Equal(2, command.Children.Count());

        var child = command.GetChild("child");
        Assert.NotNull(child);
        Assert.Equal("Child command", child.Description);

        var grandchild = child.GetChild("grandchild");
        Assert.NotNull(grandchild);
        Assert.Equal("Grandchild command", grandchild.Description);

        var child2 = command.GetChild("child2");
        Assert.NotNull(child2);
    }

    [Fact]
    public void WithArgument_AddsArgument_Correctly()
    {
        // Act
        var command = CommandBuilder.Create("test")
            .WithArgument("arg1", new StringArgumentType(), "First argument")
            .WithOptionalArgument("arg2", new IntegerArgumentType(), 42, "Second argument")
            .Build();

        // Assert
        Assert.Equal(2, command.Arguments.Count);

        var arg1 = command.Arguments[0];
        Assert.Equal("arg1", arg1.Name);
        Assert.Equal("First argument", arg1.Description);
        Assert.True(arg1.IsRequired);
        Assert.IsType<StringArgumentType>(arg1.Type);

        var arg2 = command.Arguments[1];
        Assert.Equal("arg2", arg2.Name);
        Assert.Equal("Second argument", arg2.Description);
        Assert.False(arg2.IsRequired);
        Assert.Equal(42, arg2.DefaultValue);
        Assert.IsType<IntegerArgumentType>(arg2.Type);
    }

    [Fact]
    public async Task Executes_SetsExecutor_Correctly()
    {
        // Arrange
        bool executed = false;

        // Act
        var command = CommandBuilder.Create("test")
            .Executes(context =>
            {
                executed = true;
                return CommandResult.Success;
            })
            .Build();

        var context = new CommandContext(null, "test", new Dictionary<string, object>());

        // Assert
        Assert.NotNull(command);
        var result = await command.ExecuteAsync(context);
        Assert.Equal(CommandResult.Success, result);
        Assert.True(executed);
    }

    [Fact]
    public void Build_CreatesCorrectHierarchy()
    {
        // Act
        var command = CommandBuilder.Create("test")
            .Then("sub1")
                .WithArgument("arg1", new StringArgumentType())
                .Executes(_ => CommandResult.Success)
            .EndCommand()
            .Then("sub2")
                .WithArgument("arg2", new IntegerArgumentType())
                .Executes(_ => CommandResult.Success)
            .EndCommand()
            .Build();

        // Assert
        var sub1 = command.GetChild("sub1");
        var sub2 = command.GetChild("sub2");

        Assert.NotNull(sub1);
        Assert.NotNull(sub2);
        Assert.Equal("arg1", sub1.Arguments[0].Name);
        Assert.Equal("arg2", sub2.Arguments[0].Name);
    }
}
