using Godot;
using System;

public partial class GuiStartScreen : Node
{
    [Export] public Control LoadingScreen;

    public void OnStartButtonPressed()
    {
        Core.Instance.StartGame();
        LoadingScreen.Visible = true;
    }

    public void OnQuitButtonPressed()
    {
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}
