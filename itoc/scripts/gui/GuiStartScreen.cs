using Godot;
using ITOC.Core.WorldGeneration;

namespace ITOC;

public partial class GuiStartScreen : Node
{
    [Export] public Control LoadingScreen;

    public void OnStartButtonPressed()
    {
        GameControllerNode.Instance.GenerateWorldAndStartGame();
        LoadingScreen.Visible = true;
    }

    public void OnDebugWorldButtonPressed()
    {
        GameControllerNode.Instance.GenerateWorldAndStartGame(new DebugWorldGenerator(GameControllerNode.Instance.WorldSettings));
        LoadingScreen.Visible = true;
    }

    public void OnWorld2dButtonPressed()
    {
        GameControllerNode.Instance.GotoScene("res://scenes/world_2d.tscn");
    }

    public void OnQuitButtonPressed()
    {
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}