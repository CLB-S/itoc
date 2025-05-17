using Godot;

public partial class ObjectRotator : Node2D
{
    [Export] public float MouseSensitivity = 0.01f;
    [Export] public Node3D TargetObject;

    private bool _isDragging;

    public override void _Input(InputEvent @event)
    {
        // Handle mouse button events
        if (@event is InputEventMouseButton mouseButton)
            if (mouseButton.ButtonIndex == MouseButton.Right)
                _isDragging = mouseButton.Pressed;

        // Handle mouse motion while dragging
        if (_isDragging && @event is InputEventMouseMotion mouseMotion)
        {
            // Apply rotation based on mouse movement
            TargetObject?.RotateY(mouseMotion.Relative.X * MouseSensitivity);
            TargetObject?.RotateX(mouseMotion.Relative.Y * MouseSensitivity);
        }
    }
}