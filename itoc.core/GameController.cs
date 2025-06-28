using Godot;
using ITOC.Core.Engine;
using ITOC.Core.Multithreading;
using ITOC.Core.WorldGeneration;
using ITOC.Core.WorldGeneration.Vanilla;

namespace ITOC.Core;

public class GameController : NodeAdapter
{
    public Settings Settings { get; private set; }
    public WorldSettings WorldSettings = new();

    public IWorldGenerator WorldGenerator { get; set; }
    public World CurrentWorld { get; set; }

    public static GameController Instance { get; private set; }

    /// <summary>
    /// Called before the game quited.
    /// </summary>
    public event EventHandler OnGameQuitting;

    public GameController(Node node)
        : base(node)
    {
        if (Instance != null)
            throw new Exception(
                "GameController instance already exists. Only one instance is allowed."
            );

        Instance = this;

        // Load settings
        Settings = Settings.Load();
        Settings.ApplyGraphicsSettings();

        // TaskManager.Instance.Initialize(TaskManagerConfig.Development());
        TaskManager.Instance.Initialize(TaskManagerConfig.Production());
    }

    public override void OnReady()
    {
        GD.Seed(1212);

        Node.GetWindow().MoveToCenter();
    }

    public void GenerateWorldAndStartGame(IWorldGenerator worldGenerator = null)
    {
        worldGenerator ??= new VanillaWorldGenerator();
        WorldGenerator = worldGenerator;
        WorldGenerator.Ready += (_, _) => GotoWorldScene();
        Task.Run(WorldGenerator.BeginWorldPreGeneration);
    }

    public void GotoWorldScene() => SceneSwitcher.Instance.GotoScene(BuiltInScenes.WorldScreen);

    public void GotoWorldMapScreen()
    {
        WorldGenerator ??= new VanillaWorldGenerator();
        SceneSwitcher.Instance.GotoScene(BuiltInScenes.World2dScreen);
    }

    public void QuitGame()
    {
        GD.Print("Game quitting...");

        OnGameQuitting?.Invoke(this, EventArgs.Empty);

        // Save settings on quit
        Settings.Save();

        TaskManager.Instance.Shutdown();
        TaskManager.Instance.Dispose();

        Node.GetTree().Quit();
    }

    public void PauseGame()
    {
        Node.GetTree().Paused = true;
        TaskManager.Instance.Pause();
    }

    public void ResumeGame()
    {
        if (Node.GetTree().Paused)
        {
            Node.GetTree().Paused = false;
            TaskManager.Instance.Resume();
        }
    }

    public override void OnNotification(int what)
    {
        if (what == Node.NotificationWMCloseRequest)
            QuitGame();
    }
}
