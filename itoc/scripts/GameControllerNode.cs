using System.Threading.Tasks;
using Godot;
using ITOC.Core;
using ITOC.Core.Multithreading;
using ITOC.Core.WorldGeneration;

namespace ITOC;


public enum GameState
{
    StartScreen,
    InGame,
    Paused
}

public partial class GameControllerNode : Node
{
    public GameState State { get; set; } = GameState.StartScreen;
    public Node CurrentScene { get; set; }

    public GameController GameController { get; private set; }

    public World CurrentWorld => GameController.CurrentWorld;
    public WorldGenerator WorldGenerator => GameController.WorldGenerator;
    public Settings Settings => GameController.Settings;
    public WorldSettings WorldSettings => GameController.WorldSettings;
    public TaskManager TaskManager => GameController.TaskManager;

    public static GameControllerNode Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GameController = new GameController();

        Viewport root = GetTree().Root;
        CurrentScene = root.GetChild(-1);

        GD.Seed(1212);

        GetWindow().MoveToCenter();
    }

    public void GenerateWorldAndStartGame(WorldGenerator worldGenerator = null)
    {
        if (worldGenerator != null)
            GameController.WorldGenerator = worldGenerator;

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

    public void PauseGame()
    {
        State = GameState.Paused;
        GetTree().Paused = true;
        GameController.TaskManager.Pause();
    }

    public void ResumeGame()
    {
        if (GetTree().Paused)
        {
            State = GameState.InGame;
            GetTree().Paused = false;
            GameController.TaskManager.Resume();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            QuitGame();
    }

    public void QuitGame()
    {
        // Save settings on quit
        GameController.Settings.Save();

        GameController.TaskManager.Shutdown();
        GameController.TaskManager.Dispose();

        GD.Print("Quitting game.");
        GetTree().Quit();
    }
}