using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
    [ExportGroup("Movement Settings")]
    [Export] public float MoveSpeed = 5.0f;
    [Export] public float SprintSpeed = 8.0f;
    [Export] public float JumpVelocity = 10.0f;
    [Export] public float AirControl = 0.3f;
    [Export] public float Acceleration = 10.0f;
    [Export] public float Deceleration = 15.0f;

    [ExportGroup("Camera Settings")]
    [Export] public float MouseSensitivity = 0.1f;
    [Export] public float MaxLookAngle = 89.0f;
    [Export] public float MinLookAngle = -89.0f;
    [Export] public float CameraTiltAmount = 2.5f;
    [Export] public float CameraTiltSpeed = 7.0f;

    [ExportGroup("Advanced Settings")]
    [Export] public float CoyoteTime = 0.1f;
    [Export] public float JumpBufferTime = 0.1f;

    // Nodes
    private Node3D _head;
    private Camera3D _camera;
    private Node3D _orientation;

    // Movement
    private float _currentSpeed;
    private Vector3 _direction;
    private bool _isSprinting;

    // Jump
    private float _gravity = 60 * ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    // Camera
    private float _cameraTilt;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _head = GetNode<Node3D>("Head");
        _camera = GetNode<Camera3D>("Head/Camera3D");
        _orientation = GetNode<Node3D>("Orientation");

        _currentSpeed = MoveSpeed;

        GD.Print(_gravity);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            // Horizontal rotation
            RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));

            // Vertical rotation
            _head.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
            _head.Rotation = new Vector3(
                Mathf.Clamp(_head.Rotation.X, Mathf.DegToRad(MinLookAngle),
                Mathf.DegToRad(MaxLookAngle)),
                _head.Rotation.Y,
                0
            );
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleJumpBuffer(delta);
        HandleCoyoteTime(delta);
        HandleMovement(delta);
        HandleCameraTilt(delta);
    }

    private void HandleMovement(double delta)
    {
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        _direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        // Handle sprint
        _isSprinting = Input.IsActionPressed("sprint");
        _currentSpeed = _isSprinting ? SprintSpeed : MoveSpeed;

        // Rotate direction vector relative to camera
        Vector3 rotatedDirection = _orientation.GlobalTransform.Basis * _direction;

        Vector3 targetVelocity = rotatedDirection * _currentSpeed;
        targetVelocity.Y = Velocity.Y;

        // Apply gravity
        if (!IsOnFloor())
        {
            targetVelocity.Y -= (float)(_gravity * delta);
        }

        // Calculate acceleration rate
        float accel = IsOnFloor() ? Acceleration : Acceleration * AirControl;
        float decel = IsOnFloor() ? Deceleration : Deceleration * AirControl;

        // Interpolate velocity
        Velocity = Velocity.Lerp(targetVelocity,
            (rotatedDirection.LengthSquared() > 0 ? accel : decel) * (float)delta);

        MoveAndSlide();

        // Jump handling - now properly using jump buffer
        if (_jumpBufferTimer > 0 && CanJump())
        {
            Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);
            _coyoteTimer = 0;
            _jumpBufferTimer = 0;
        }
    }

    private bool CanJump()
    {
        return IsOnFloor() || _coyoteTimer > 0;
    }

    private void HandleCoyoteTime(double delta)
    {
        if (IsOnFloor())
        {
            _coyoteTimer = CoyoteTime;
        }
        else
        {
            _coyoteTimer -= (float)delta;
        }
    }

    private void HandleJumpBuffer(double delta)
    {
        if (Input.IsActionJustPressed("jump"))
        {
            _jumpBufferTimer = JumpBufferTime;
        }
        else if (_jumpBufferTimer > 0)
        {
            _jumpBufferTimer -= (float)delta;
        }
    }

    private void HandleCameraTilt(double delta)
    {
        float targetTilt = 0;

        if (Input.IsActionPressed("move_left"))
        {
            targetTilt += CameraTiltAmount;
        }
        if (Input.IsActionPressed("move_right"))
        {
            targetTilt -= CameraTiltAmount;
        }

        _cameraTilt = Mathf.Lerp(_cameraTilt, targetTilt, CameraTiltSpeed * (float)delta);
        _camera.Rotation = new Vector3(_camera.Rotation.X, _camera.Rotation.Y,
            Mathf.DegToRad(_cameraTilt));
    }
}