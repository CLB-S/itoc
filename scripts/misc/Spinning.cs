using Godot;

public partial class Spinning : MeshInstance3D
{
    [Export]
    public float SpinningSpeed = 1f;
    [Export]
    public Vector3 SpinningAxis = Vector3.Up;

    public override void _Process(double delta)
    {
        this.Rotate(SpinningAxis, (float)delta * SpinningSpeed);
    }
}