using Godot;
using ITOC.Core.Multithreading;
using ITOC.Core.WorldGeneration;

namespace ITOC.Core;


public class GameController
{
    public Settings Settings { get; private set; }
    public WorldSettings WorldSettings = new();

    public Node CurrentScene { get; set; }
    public WorldGenerator WorldGenerator { get; set; }
    public TaskManager TaskManager { get; private set; }
    public World CurrentWorld { get; set; }

    public GameController()
    {
        // Load settings
        Settings = Settings.Load();
        Settings.ApplyGraphicsSettings();

        WorldGenerator = new WorldGenerator(WorldSettings);
        TaskManager = TaskManager.Instance;
        // TaskManager.Initialize(TaskManagerConfig.Development());
        TaskManager.Initialize(TaskManagerConfig.Production());
    }
}