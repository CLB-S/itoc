using Godot;

namespace ITOC.Core.Engine;

public abstract class NodeAdapter
{
    public Node Node { get; }

    public NodeAdapter(Node node) =>
        Node = node ?? throw new ArgumentNullException(nameof(node), "Node cannot be null.");

    public virtual void OnEnterTree() { }

    public virtual void OnExitTree() { }

    public virtual void OnReady() { }

    public virtual void OnProcess(double delta) { }

    public virtual void OnPhysicsProcess(double delta) { }

    public virtual void OnInput(InputEvent inputEvent) { }

    public virtual void OnShortcutInput(InputEvent inputEvent) { }

    public virtual void OnUnhandledInput(InputEvent inputEvent) { }

    public virtual void OnUnhandledKeyInput(InputEvent inputEvent) { }

    public virtual void OnNotification(int what) { }
}
