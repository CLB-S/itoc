using Godot;
using ITOC.Core;
using ITOC.Core.WorldGeneration.Infinite;
using ITOC.Core.WorldGeneration.Vanilla;

namespace ITOC;

public partial class GuiStartScreen : Node
{
    [Export] public Control LoadingScreen;

    public void OnStartButtonPressed()
    {
        GameController.Instance.GenerateWorldAndStartGame(new VanillaWorldGenerator());
        LoadingScreen.Visible = true;
    }

    public void OnDebugWorldButtonPressed()
    {
        GameController.Instance.GenerateWorldAndStartGame(new InfiniteWorldGenerator());
        LoadingScreen.Visible = true;
    }

    public void OnWorld2dButtonPressed()
    {
        GameController.Instance.GotoWorldMapScreen();
    }

    public void OnQuitButtonPressed()
    {
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}