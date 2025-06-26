using ITOC.Core.Command;

namespace ITOC.Test.Command;

public class CommandNodeTests
{
    [Fact]
    public void Constructor_ValidatesName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CommandNode(""));
        Assert.Throws<ArgumentException>(() => new CommandNode(null));
        Assert.Throws<ArgumentException>(() => new CommandNode("invalid name"));
        Assert.Throws<ArgumentException>(() => new CommandNode("Invalid"));

        // These should not throw
        _ = new CommandNode("valid");
        _ = new CommandNode("valid-name");
        _ = new CommandNode("valid_name");
        _ = new CommandNode("valid.name");
        _ = new CommandNode("valid123");
    }

    [Fact]
    public void HasPermission_Validates_Correctly()
    {
        // Arrange
        var command = new CommandNode("test", permission: "test.permission");
        var holder = new TestPermissionHolder("test.permission");
        var holderNoPermission = new TestPermissionHolder("other.permission");

        // Act & Assert
        Assert.True(command.HasPermission(holder));
        Assert.False(command.HasPermission(holderNoPermission));

        var noPermissionCommand = new CommandNode("test");
        Assert.True(noPermissionCommand.HasPermission(null));
    }

    [Fact]
    public void Then_AddsSubCommand_Correctly()
    {
        // Arrange
        var parent = new CommandNode("parent");
        var child = new CommandNode("child");

        // Act
        parent.Then(child);

        // Assert
        Assert.Single(parent.Children);
        Assert.Equal(child, parent.GetChild("child"));
        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void Then_ThrowsOnDuplicateName()
    {
        // Arrange
        var parent = new CommandNode("parent");
        var child1 = new CommandNode("child");
        var child2 = new CommandNode("child");

        // Act
        parent.Then(child1);

        // Assert
        Assert.Throws<ArgumentException>(() => parent.Then(child2));
    }

    [Fact]
    public void GetChild_FindsAliases()
    {
        // Arrange
        var parent = new CommandNode("parent");
        var child = new CommandNode("child", aliases: new[] { "c", "kid" });

        // Act
        parent.Then(child);

        // Assert
        Assert.Equal(child, parent.GetChild("child"));
        Assert.Equal(child, parent.GetChild("c"));
        Assert.Equal(child, parent.GetChild("kid"));
        Assert.Null(parent.GetChild("unknown"));
    }

    [Fact]
    public async Task ExecuteAsync_WorksCorrectly()
    {
        // Arrange
        var executed = false;
        var command = new CommandNode("test");
        command.SetExecutor(ctx =>
        {
            executed = true;
            return Task.FromResult(CommandResult.Success);
        });
        var context = new CommandContext(null, "test", new Dictionary<string, object>());

        // Act
        var result = await command.ExecuteAsync(context);

        // Assert
        Assert.True(executed);
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenNoExecutor()
    {
        // Arrange
        var command = new CommandNode("test");
        var context = new CommandContext(null, "test", new Dictionary<string, object>());

        // Act
        var result = await command.ExecuteAsync(context);

        // Assert
        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsPermissions()
    {
        // Arrange
        var command = new CommandNode("test", permission: "test.permission");
        command.SetExecutor(_ => Task.FromResult(CommandResult.Success));
        var context = new CommandContext(
            new TestPermissionHolder(),
            "test",
            new Dictionary<string, object>()
        );

        // Act
        var result = await command.ExecuteAsync(context);

        // Assert
        Assert.Equal(CommandResult.PermissionDenied, result);
    }
}
