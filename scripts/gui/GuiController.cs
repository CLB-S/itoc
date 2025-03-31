using Godot;
using System.Collections.Generic;

public partial class GuiController : Control
{
    protected static GuiState[] PausingStates = [GuiState.Settings, GuiState.Paused];

    public virtual void OnEnter()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = true;
        GetTree().Paused = ShouldPauseGame();
        UpdateMouseState();
    }

    public virtual void OnExit()
    {
        Visible = false;
        GetTree().Paused = false;
        UpdateMouseState();
    }

    protected bool ShouldPauseGame()
    {
        foreach (var state in PausingStates)
        {
            if (GuiManager.Instance.CurrentState == state) return true;
        }
        return false;
    }

    protected void UpdateMouseState()
    {
        Input.MouseMode = Visible ?
            Input.MouseModeEnum.Visible :
            Input.MouseModeEnum.Captured;
    }
}