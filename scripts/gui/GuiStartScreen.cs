using Godot;

public partial class GuiStartScreen : Node
{
    [Export] public Control LoadingScreen;

    public void OnStartButtonPressed()
    {
        Core.Instance.GenerateWorldAndStartGame();
        LoadingScreen.Visible = true;
    }

    public void OnDebugWorldButtonPressed()
    {
        Core.Instance.GenerateWorldAndStartGame(new WorldGenerator.DebugWorldGenerator(Core.Instance.WorldSettings));
        LoadingScreen.Visible = true;
    }

    public void OnWorld2dButtonPressed()
    {
        Core.Instance.GotoScene("res://scenes/world_2d.tscn");
    }

    public void OnQuitButtonPressed()
    {
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}