using Godot;

namespace ITOC.Core.Engine;

public static class BuiltInScenes
{
    public const string StartScreen = "res://scenes/start_screen.tscn";
    public const string WorldScreen = "res://scenes/world.tscn";
    public const string World2dScreen = "res://scenes/world_2d.tscn";
}

public class SceneSwitcher : NodeAdapter
{
    public Node CurrentScene { get; set; }
    public static SceneSwitcher Instance { get; private set; }

    public SceneSwitcher(Node node) : base(node)
    {
        if (Instance != null)
            throw new Exception("SceneSwitcher instance already exists. Only one instance is allowed.");

        Instance = this;
    }

    public override void OnReady()
    {
        Viewport root = Node.GetTree().Root;
        CurrentScene = root.GetChild(-1);

        base.OnReady();
    }

    public void GotoScene(string path)
    {
        GD.Print($"Switching to scene: {path}");

        // This function will usually be called from a signal callback,
        // or some other function from the current scene.
        // Deleting the current scene at this point is
        // a bad idea, because it may still be executing code.
        // This will result in a crash or unexpected behavior.

        // The solution is to defer the load to a later time, when
        // we can be sure that no code from the current scene is running:
        Callable.From(new Action(() =>
        {
            // It is now safe to remove the current scene.
            CurrentScene.Free();

            var nextScene = GD.Load<PackedScene>(path);
            CurrentScene = nextScene.Instantiate();
            Node.GetTree().Root.AddChild(CurrentScene);
            Node.GetTree().CurrentScene = CurrentScene;

            GD.Print($"Scene switched to: {path}");
        }
        )).CallDeferred();
    }
}