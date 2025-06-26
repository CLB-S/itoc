using Godot;
using ITOC.Core;

namespace ITOC;

public enum GuiState
{
    Gameplay,
    Paused,
    Inventory,
    Settings,
    Dialogue,
}

public partial class GuiController : Control
{
    protected static GuiState[] PausingStates = [GuiState.Settings, GuiState.Paused];

    public virtual void OnEnter()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = true;
        if (ShouldPauseGame())
            GameController.Instance.PauseGame();
        UpdateMouseState();
    }

    public virtual void OnExit()
    {
        Visible = false;
        GameController.Instance.ResumeGame();
        UpdateMouseState();
    }

    protected static bool ShouldPauseGame()
    {
        foreach (var state in PausingStates)
            if (GuiManager.Instance.CurrentState == state)
                return true;
        return false;
    }

    protected void UpdateMouseState() =>
        Input.MouseMode = Visible ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
}
