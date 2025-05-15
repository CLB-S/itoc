using System;
using Godot;

public partial class PlayerController : CharacterBody3D
{
    [ExportGroup("Movement Settings")]
    [Export] public float MoveSpeed = 5.0f;
    [Export] public float SprintSpeed = 8.0f;
    [Export] public float JumpVelocity = 9.0f;
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

    [ExportGroup("Flying Settings")]
    [Export] public float FlyingSpeed = 20.0f;
    [Export] public float FlyingSprintSpeed = 200.0f; // TODO: Move to user settings.
    [Export] public float FlyingAcceleration = 15.0f;
    [Export] public float FlyingVerticalSpeed = 7.0f;
    [Export] public float DoubleTapThreshold = 0.25f; // Time window for double tap

    [ExportGroup("Block Interaction Settings")]
    [Export] public float BlockInteractionMaxDistance = 5.0f;

    [ExportGroup("GUI")]
    [Export] public GuiHud Hud;

    // Nodes
    private Node3D _head;
    private Camera3D _camera;
    private Node3D _orientation;

    // Movement
    private float _currentSpeed;
    private Vector3 _direction;
    private bool _isSprinting;

    // Jump
    private float _gravity = 3 * ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    // Camera
    private float _cameraTilt;

    // Flying
    private bool _isFlying;
    private float _lastJumpPressTime = -1f;

    // Inventory
    public IItem ItemHandhelding => Hud.InventoryHotbar.ActiveItem;

    public override void _Ready()
    {
        // Input.MouseMode = Input.MouseModeEnum.Captured;

        _head = GetNode<Node3D>("Head");
        _camera = GetNode<Camera3D>("Head/Camera3D");
        _orientation = GetNode<Node3D>("Orientation");

        _currentSpeed = MoveSpeed;
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

        HandleInventoryHotbarInput(@event);
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleJumpBuffer(delta);
        HandleCoyoteTime(delta);

        if (_isFlying)
            HandleFlyingMovement(delta);
        else
            HandleMovement(delta);

        HandleCameraTilt(delta);

        HandleBlockInteractions(delta);
    }

    private void HandleBlockInteractions(double delta)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var mousePos = GetViewport().GetMousePosition();

        var origin = _camera.ProjectRayOrigin(mousePos);
        var end = origin + _camera.ProjectRayNormal(mousePos) * BlockInteractionMaxDistance;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.CollideWithAreas = true;

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
            try
            {
                // var pos = result["position"].AsVector3() - 0.5f * result["normal"].AsVector3();
                // var block = World.Instance.GetBlock(pos);
                // GD.Print($"Looking at block {block}:{BlockManager.Instance.GetBlock(block).BlockName}");
                var placeBlockPressed = Input.IsActionJustPressed("place_block");
                var breakBlockPressed = Input.IsActionJustPressed("break_block");

                if (placeBlockPressed)
                {
                    var pos = result["position"].AsVector3() + 0.5f * result["normal"].AsVector3();

                    var cubeShape = new BoxShape3D { Size = Vector3.One * 0.95f };
                    var cubeCenter = new Vector3(Mathf.Floor(pos.X) + 0.5f, Mathf.Floor(pos.Y) + 0.5f,
                        Mathf.Floor(pos.Z) + 0.5f);
                    var cubeTransform = new Transform3D { Origin = cubeCenter };

                    var queryCube = new PhysicsShapeQueryParameters3D
                    {
                        CollideWithAreas = true,
                        Shape = cubeShape,
                        Transform = cubeTransform
                    };

                    var resultCube = spaceState.IntersectShape(queryCube, 1);
                    if (resultCube.Count == 0 && ItemHandhelding?.Type == ItemType.Block)
                        Core.Instance.CurrentWorld.SetBlock(pos, ItemHandhelding as Block);
                }

                if (breakBlockPressed)
                {
                    var pos = result["position"].AsVector3() - 0.5f * result["normal"].AsVector3();
                    Core.Instance.CurrentWorld.SetBlock(pos, (Block)null);
                }
            }
            catch (Exception e)
            {
                GD.PushError(e.ToString());
            }
    }

    private void HandleInventoryHotbarInput(InputEvent @event)
    {
        for (var i = 1; i <= 9; i++)
            if (@event.IsActionPressed($"hotbar{i}"))
                Hud.InventoryHotbar.SetActiveSlot(i - 1);

        if (@event.IsActionPressed("hotbar_next"))
            Hud.InventoryHotbar.NextSlot();
        else if (@event.IsActionPressed("hotbar_prev")) Hud.InventoryHotbar.PreviousSlot();
    }

    private void HandleMovement(double delta)
    {
        var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        _direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        // Handle sprint
        _isSprinting = Input.IsActionPressed("sprint");
        _currentSpeed = _isSprinting ? SprintSpeed : MoveSpeed;

        // Rotate direction vector relative to camera
        var rotatedDirection = _orientation.GlobalTransform.Basis * _direction;

        // Calculate horizontal and vertical components separately
        var horizontalVelocity = new Vector3(Velocity.X, 0, Velocity.Z);
        var verticalVelocity = Velocity.Y;

        // Target horizontal velocity (air control doesn't affect vertical movement)
        var targetHorizontalVelocity = rotatedDirection * _currentSpeed;
        var targetVerticalVelocity = verticalVelocity;

        // Apply gravity (always full strength, unaffected by air control)
        if (!IsOnFloor()) targetVerticalVelocity -= (float)(_gravity * delta);

        // Calculate acceleration rates
        var accel = IsOnFloor() ? Acceleration : Acceleration * AirControl;
        var decel = IsOnFloor() ? Deceleration : Deceleration * AirControl;

        // Only apply air control to horizontal movement
        horizontalVelocity = horizontalVelocity.Lerp(
            targetHorizontalVelocity,
            (rotatedDirection.LengthSquared() > 0 ? accel : decel) * (float)delta
        );

        // Combine components back into final velocity
        Velocity = new Vector3(horizontalVelocity.X, targetVerticalVelocity, horizontalVelocity.Z);

        MoveAndSlide();

        // Jump handling
        if (_jumpBufferTimer > 0 && CanJump())
        {
            Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);
            _coyoteTimer = 0;
            _jumpBufferTimer = 0;
        }
    }

    private void HandleFlyingMovement(double delta)
    {
        var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        _direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
        _isSprinting = Input.IsActionPressed("sprint");

        // Handle vertical movement for flying
        var verticalInput = 0f;
        if (Input.IsActionPressed("jump")) verticalInput = 1f;
        if (Input.IsActionPressed("sneak")) verticalInput = -1f;

        // Rotate direction vector relative to camera
        var rotatedDirection = _orientation.GlobalTransform.Basis * _direction;
        rotatedDirection.Y = verticalInput;

        var targetVelocity = rotatedDirection * (_isSprinting ? FlyingSprintSpeed : FlyingSpeed);

        // Interpolate velocity
        Velocity = Velocity.Lerp(targetVelocity, FlyingAcceleration * (float)delta);

        MoveAndSlide();
    }

    private bool CanJump()
    {
        return IsOnFloor() || _coyoteTimer > 0;
    }

    private void HandleCoyoteTime(double delta)
    {
        if (IsOnFloor())
            _coyoteTimer = CoyoteTime;
        else
            _coyoteTimer -= (float)delta;
    }

    private void HandleJumpBuffer(double delta)
    {
        if (Input.IsActionJustPressed("jump"))
        {
            // Check for double tap
            var currentTime = Time.GetTicksMsec() / 1000f;
            if (currentTime - _lastJumpPressTime < DoubleTapThreshold)
            {
                _isFlying = !_isFlying;
                if (_isFlying) Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
            }

            _lastJumpPressTime = currentTime;

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

        if (Input.IsActionPressed("move_left")) targetTilt += CameraTiltAmount;
        if (Input.IsActionPressed("move_right")) targetTilt -= CameraTiltAmount;

        _cameraTilt = Mathf.Lerp(_cameraTilt, targetTilt, CameraTiltSpeed * (float)delta);
        _camera.Rotation = new Vector3(_camera.Rotation.X, _camera.Rotation.Y,
            Mathf.DegToRad(_cameraTilt));
    }
}