using Godot;
using ITOC.Core;

namespace ITOC;

public partial class GuiPausedController : GuiController
{
    public override void _Ready()
    {
        base._Ready();
        GuiManager.Instance.GuiControllers[GuiState.Paused] = this;
    }

    public void OnBackToGameButtonPressed()
    {
        GuiManager.Instance.CloseCurrentUI();
    }

    public void OnQuitGameButtonPressed()
    {
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}