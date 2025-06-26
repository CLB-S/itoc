using Godot;
using ITOC.Core;
using ITOC.Core.Multithreading;

namespace ITOC;

public partial class GameControllerNode : Node
{
    public GameController GameController { get; private set; }

    public override void _Ready()
    {
        GameController = new GameController(this);

        GameController.OnReady();
    }

    public void PauseGame()
    {
        GetTree().Paused = true;
        TaskManager.Instance.Pause();
    }

    public void ResumeGame()
    {
        if (GetTree().Paused)
        {
            GetTree().Paused = false;
            TaskManager.Instance.Resume();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            GameController.QuitGame();
    }
}