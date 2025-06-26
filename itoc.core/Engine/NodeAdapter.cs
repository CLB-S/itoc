using Godot;

namespace ITOC.Core.Engine;

public abstract class NodeAdapter
{
    public Node Node { get; }

    public NodeAdapter(Node node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node), "Node cannot be null.");
    }

    public event EventHandler OnEnterTreeEvent;
    public event EventHandler OnExitTreeEvent;
    public event EventHandler Ready;
    public event EventHandler<double> Process;
    public event EventHandler<double> PhysicsProcess;
    public event EventHandler<InputEvent> OnInputEvent;
    public event EventHandler<InputEvent> OnShortcutInputEvent;
    public event EventHandler<InputEvent> OnUnhandledInputEvent;
    public event EventHandler<InputEvent> OnUnhandledKeyInputEvent;
    public event EventHandler<int> OnNotificationEvent;

    public virtual void OnEnterTree() => OnEnterTreeEvent?.Invoke(this, EventArgs.Empty);
    public virtual void OnExitTree() => OnExitTreeEvent?.Invoke(this, EventArgs.Empty);
    public virtual void OnReady() => Ready?.Invoke(this, EventArgs.Empty);
    public virtual void OnProcess(double delta) => Process?.Invoke(this, delta);
    public virtual void OnPhysicsProcess(double delta) => PhysicsProcess?.Invoke(this, delta);
    public virtual void OnInput(InputEvent inputEvent) => OnInputEvent?.Invoke(this, inputEvent);
    public virtual void OnShortcutInput(InputEvent inputEvent) => OnShortcutInputEvent?.Invoke(this, inputEvent);
    public virtual void OnUnhandledInput(InputEvent inputEvent) => OnUnhandledInputEvent?.Invoke(this, inputEvent);
    public virtual void OnUnhandledKeyInput(InputEvent inputEvent) => OnUnhandledKeyInputEvent?.Invoke(this, inputEvent);
    public virtual void OnNotification(int notification) => OnNotificationEvent?.Invoke(this, notification);
}