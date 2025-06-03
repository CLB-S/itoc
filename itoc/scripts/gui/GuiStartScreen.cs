using Godot;
using ITOC.Core.WorldGeneration;
using ITOC.Core.WorldGeneration.Infinite;

namespace ITOC;

public partial class GuiStartScreen : Node
{
    [Export] public Control LoadingScreen;

    public void OnStartButtonPressed()
    {
        GameControllerNode.Instance.GenerateWorldAndStartGame(new WorldGenerator());
        LoadingScreen.Visible = true;
    }

    public void OnDebugWorldButtonPressed()
    {
        GameControllerNode.Instance.GenerateWorldAndStartGame(new InfiniteWorldGenerator());
        LoadingScreen.Visible = true;
    }

    public void OnWorld2dButtonPressed()
    {
        GameControllerNode.Instance.GotoWorldMapScreen();
    }

    public void OnQuitButtonPressed()
    {
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}