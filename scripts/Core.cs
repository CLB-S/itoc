using System.Threading.Tasks;
using Godot;

public enum GameState
{
    StartScreen,
    InGame,
    Paused
}

public partial class Core : Node
{
    public Settings Settings { get; private set; }
    public WorldSettings WorldSettings = new();

    public Node CurrentScene { get; set; }
    public GameState State { get; set; } = GameState.StartScreen;
    public WorldGenerator.WorldGenerator WorldGenerator { get; private set; }

    public static Core Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        Viewport root = GetTree().Root;
        CurrentScene = root.GetChild(-1);

        GD.Seed(1212);

        // Load settings
        Settings = Settings.Load();
        Settings.ApplyGraphicsSettings();
        GetWindow().MoveToCenter();

        WorldGenerator = new WorldGenerator.WorldGenerator(WorldSettings);
    }

    public void GenerateWorldAndStartGame()
    {
        WorldGenerator.GenerationCompletedEvent += (_, _) => CallDeferred(MethodName.GotoWorldScene);
        Task.Run(WorldGenerator.GenerateWorldAsync);
    }

    public void GotoWorldScene()
    {
        GotoScene("res://scenes/world.tscn");
    }

    public void GotoScene(string path)
    {
        // This function will usually be called from a signal callback,
        // or some other function from the current scene.
        // Deleting the current scene at this point is
        // a bad idea, because it may still be executing code.
        // This will result in a crash or unexpected behavior.

        // The solution is to defer the load to a later time, when
        // we can be sure that no code from the current scene is running:
        CallDeferred(MethodName.DeferredGotoScene, path);
    }

    public void DeferredGotoScene(string path)
    {
        // It is now safe to remove the current scene.
        CurrentScene.Free();

        var nextScene = GD.Load<PackedScene>(path);
        CurrentScene = nextScene.Instantiate();
        GetTree().Root.AddChild(CurrentScene);
        GetTree().CurrentScene = CurrentScene;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            QuitGame();
    }

    public void QuitGame()
    {
        // Save settings on quit
        Settings.Save();
        GD.Print("Quitting game.");
        GetTree().Quit();
    }
}