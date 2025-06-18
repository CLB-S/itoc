using Godot;
using ITOC.Core.Multithreading;
using ITOC.Core.WorldGeneration;

namespace ITOC.Core;


public class GameController
{
    public Settings Settings { get; private set; }
    public WorldSettings WorldSettings = new();

    public Node CurrentScene { get; set; }
    public IWorldGenerator WorldGenerator { get; set; }
    public World CurrentWorld { get; set; }

    public GameController()
    {
        // Load settings
        Settings = Settings.Load();
        Settings.ApplyGraphicsSettings();

        // TaskManager.Instance.Initialize(TaskManagerConfig.Development());
        TaskManager.Instance.Initialize(TaskManagerConfig.Production());
    }
}