using Godot;
using ITOC.Core;

namespace ITOC;

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

        if (CheckCamera())
        {
            CameraPosition = _camera.GlobalTransform.Origin;
            CameraFacing = -_camera.GlobalTransform.Basis.Z;
            CameraFacingDirection = DirectionHelper.GetDirection(CameraFacing);
        }
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

    /// <summary>
    /// Calculates the distance at which a 1 meter line would occupy the specified number of pixels on screen.
    /// </summary>
    /// <param name="pixelCount">Target number of pixels the object should occupy</param>
    /// <param name="lineLength">Length of the object in world units (default is 1.0)</param>
    /// <returns>The distance threshold in world units</returns>
    public double CalculateDistanceThresholdForPixels(double pixelCount, double lineLength = 1.0)
    {
        if (!CheckCamera())
            return 0f;

        // Get viewport size and camera FOV
        Vector2 viewportSize = GetViewportSize();
        var fovY = _camera.Fov * Mathf.Pi / 180.0; // Convert to radians

        // Calculate distance based on the number of pixels a 1m object should occupy
        var viewHeight = 2.0 * Mathf.Tan(fovY / 2.0);
        var pixelSize = viewHeight / viewportSize.Y;

        // Distance = object size / (pixel count * pixel size)
        return lineLength / (pixelCount * pixelSize);
    }

    /// <summary>
    /// Converts a world position to screen coordinates
    /// </summary>
    /// <param name="worldPosition">Position in 3D world space</param>
    /// <returns>Screen coordinates (in pixels)</returns>
    public Vector2 WorldToScreen(Vector3 worldPosition)
    {
        if (!CheckCamera())
            return Vector2.Zero;

        return _camera.UnprojectPosition(worldPosition);
    }

    /// <summary>
    /// Gets the current viewport size
    /// </summary>
    /// <returns>Viewport dimensions in pixels</returns>
    public Vector2 GetViewportSize()
    {
        return GetViewport().GetVisibleRect().Size;
    }
}