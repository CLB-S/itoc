using Godot;
using System;

// Singleton class to manage the camera
public partial class CameraHelper : Node
{
    public static CameraHelper Instance { get; private set; }

    public Vector3 CameraPosition = Vector3.Zero;
    public Vector3 CameraFacing = Vector3.Zero;
    public Direction CameraFacingDirection = Direction.PositiveZ;

    private Camera3D _camera;

    private bool CheckCamera()
    {
        if (_camera == null || !_camera.IsInsideTree())
            _camera = GetViewport().GetCamera3D();

        return _camera != null && _camera.IsInsideTree();
    }


    public override void _Ready()
    {
        Instance = this;
        _camera = GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (CheckCamera())
        {
            CameraPosition = _camera.GlobalTransform.Origin;
            CameraFacing = -_camera.GlobalTransform.Basis.Z;
            CameraFacingDirection = DirectionHelper.GetDirection(CameraFacing);
        }
    }

    public Vector3 GetCameraPosition() => CameraPosition;

    public Vector3 GetCameraFacing() => CameraFacing;

    public Direction GetCameraFacingDirection() => CameraFacingDirection;

}