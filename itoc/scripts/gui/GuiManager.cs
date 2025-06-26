using System.Collections.Generic;
using Godot;

namespace ITOC;

public partial class GuiManager : Node
{
    private readonly Stack<GuiState> _stateStack = new();

    public Dictionary<GuiState, GuiController> GuiControllers = new();
    public static GuiManager Instance { get; private set; }

    public GuiState CurrentState { get; private set; } = GuiState.Gameplay;

    public override void _Ready()
    {
        Instance = this;

        SetProcessInput(true);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            HandleEscapeInput();
    }

    private void HandleEscapeInput()
    {
        switch (CurrentState)
        {
            case GuiState.Gameplay:
                OpenUI(GuiState.Paused);
                break;
            case GuiState.Paused:
                CloseCurrentUI();
                break;
            case GuiState.Settings:
                CloseCurrentUI();
                break;
        }
    }

    public void OpenUI(GuiState newState)
    {
        _stateStack.Push(CurrentState);
        ChangeState(newState);
    }

    public void CloseCurrentUI()
    {
        if (_stateStack.Count > 0)
            ChangeState(_stateStack.Pop());
        else
            ChangeState(GuiState.Gameplay);
    }

    private void ChangeState(GuiState newState)
    {
        if (CurrentState == newState)
            return;

        if (GuiControllers.TryGetValue(CurrentState, out var currentUI))
            currentUI.OnExit();

        CurrentState = newState;

        if (GuiControllers.TryGetValue(newState, out var newUI))
            newUI.OnEnter();
    }
}
