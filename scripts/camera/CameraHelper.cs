using Godot;

// Singleton class to manage the camera
public partial class CameraHelper : Node
{
    private Camera3D _camera;
    public Vector3 CameraFacing = Vector3.Zero;
    public Direction CameraFacingDirection = Direction.PositiveZ;

    public Vector3 CameraPosition = Vector3.Zero;
    public static CameraHelper Instance { get; private set; }

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

        // if (CheckCamera())
        // {
        CameraPosition = _camera.GlobalTransform.Origin;
        CameraFacing = -_camera.GlobalTransform.Basis.Z;
        CameraFacingDirection = DirectionHelper.GetDirection(CameraFacing);
        // }
    }

    public Camera3D GetActiveCamera()
    {
        return _camera;
    }


    public Vector3 GetCameraPosition()
    {
        return CameraPosition;
    }

    public Vector3 GetCameraFacing()
    {
        return CameraFacing;
    }

    public Direction GetCameraFacingDirection()
    {
        return CameraFacingDirection;
    }
}