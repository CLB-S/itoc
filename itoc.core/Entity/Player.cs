using Godot;
using ITOC.Core.Engine;

namespace ITOC.Core.Entity;

public class Player : NodeAdapter
{
    private CharacterBody3D _characterBody => Node as CharacterBody3D;

    // Movement Settings
    public double MoveSpeed = 5.0;
    public double SprintSpeed = 8.0;
    public double JumpVelocity = 9.0;
    public double AirControl = 0.3;
    public double Acceleration = 10.0;
    public double Deceleration = 15.0;

    // Camera Settings
    public double MouseSensitivity = 0.1;
    public double MaxLookAngle = 89.0;
    public double MinLookAngle = -89.0;
    public double CameraTiltAmount = 2.5;
    public double CameraTiltSpeed = 7.0;

    // Advanced Settings
    public double CoyoteTime = 0.1;
    public double JumpBufferTime = 0.1;

    // Flying Settings
    public double FlyingSpeed = 20.0;
    public double FlyingSprintSpeed = 200.0; // TODO: Move to user settings.
    public double FlyingAcceleration = 15.0;
    public double FlyingVerticalSpeed = 7.0;
    public double DoubleTapThreshold = 0.25; // Time window for double tap

    // Block Interaction Settings
    public double BlockInteractionMaxDistance = 5.0;

    // Events
    public event EventHandler<Vector3> OnChunkLoadingTriggered;

    // Nodes
    private Node3D _head;
    private Camera3D _camera;
    private Node3D _orientation;

    // Movement
    private double _currentSpeed;
    private Vector3 _direction;
    private bool _isSprinting;

    // Jump
    private readonly double _gravity =
        3 * ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private double _coyoteTimer;
    private double _jumpBufferTimer;

    // Camera
    private double _cameraTilt;

    // Flying
    private bool _isFlying = true;
    private double _lastJumpPressTime = -1;

    // Chunk Loading
    private Vector3 _lastPosition = Vector3.Inf;
    private readonly double _distanceBetweenChunkLoadingUpdates = Chunk.SIZE / 2.0;
    private readonly ChunkRange _chunkLoadingRange;
    private readonly ChunkLoadingSource _chunkLoadingSource;

    public Player(Node node)
        : base(node)
    {
        if (node is not CharacterBody3D)
            throw new ArgumentException("Node must be a CharacterBody3D", nameof(node));

        _chunkLoadingRange = new ChunkRange(8, 3);
        _chunkLoadingSource = new ChunkLoadingSource(
            _chunkLoadingRange,
            GameController.Instance.WorldGenerator.ChunkGenerator
        );
    }

    public override void OnReady()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _head = Node.GetNode<Node3D>("Head");
        _camera = Node.GetNode<Camera3D>("Head/Camera3D");
        _orientation = Node.GetNode<Node3D>("Orientation");

        _currentSpeed = MoveSpeed;

        base.OnReady();
    }

    public override void OnInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            // Horizontal rotation
            _characterBody.RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));

            // Vertical rotation
            _head.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
            _head.Rotation = new Vector3(
                Mathf.Clamp(
                    _head.Rotation.X,
                    Mathf.DegToRad(MinLookAngle),
                    Mathf.DegToRad(MaxLookAngle)
                ),
                _head.Rotation.Y,
                0
            );
        }

        // HandleInventoryHotbarInput(@event);

        base.OnInput(@event);
    }

    public override void OnPhysicsProcess(double delta)
    {
        HandleJumpBuffer(delta);
        HandleCoyoteTime(delta);

        if (_isFlying)
            HandleFlyingMovement(delta);
        else
            HandleMovement(delta);

        HandleCameraTilt(delta);

        HandleBlockInteractions();

        HandleChunkLoading();

        base.OnPhysicsProcess(delta);
    }

    private void HandleChunkLoading()
    {
        var currentPosition = _characterBody.GlobalPosition;
        if (currentPosition.DistanceTo(_lastPosition) >= _distanceBetweenChunkLoadingUpdates)
        {
            _chunkLoadingSource.UpdateFrom(currentPosition);
            _lastPosition = currentPosition;

            OnChunkLoadingTriggered?.Invoke(this, currentPosition);
        }
    }

    private void HandleBlockInteractions()
    {
        var spaceState = _characterBody.GetWorld3D().DirectSpaceState;
        var mousePos = _characterBody.GetViewport().GetMousePosition();

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
                    var cubeCenter = new Vector3(
                        Mathf.Floor(pos.X) + 0.5f,
                        Mathf.Floor(pos.Y) + 0.5f,
                        Mathf.Floor(pos.Z) + 0.5f
                    );
                    var cubeTransform = new Transform3D { Origin = cubeCenter };

                    var queryCube = new PhysicsShapeQueryParameters3D
                    {
                        CollideWithAreas = true,
                        Shape = cubeShape,
                        Transform = cubeTransform,
                    };

                    var resultCube = spaceState.IntersectShape(queryCube, 1);
                    // if (resultCube.Count == 0 && ItemHandhelding?.Type == ItemType.Block)
                    //     GameController.Instance.CurrentWorld.SetBlock(pos, ItemHandhelding as Block);
                }

                if (breakBlockPressed)
                {
                    var pos = result["position"].AsVector3() - 0.5f * result["normal"].AsVector3();
                    // GameController.Instance.CurrentWorld.SetBlock(pos, (Block)null);
                }
            }
            catch (Exception e)
            {
                GD.PushError(e.ToString());
            }
    }

    // private void HandleInventoryHotbarInput(InputEvent @event)
    // {
    //     for (var i = 1; i <= 9; i++)
    //         if (@event.IsActionPressed($"hotbar{i}"))
    //             Hud.InventoryHotbar.SetActiveSlot(i - 1);

    //     if (@event.IsActionPressed("hotbar_next"))
    //         Hud.InventoryHotbar.NextSlot();
    //     else if (@event.IsActionPressed("hotbar_prev")) Hud.InventoryHotbar.PreviousSlot();
    // }

    private void HandleMovement(double delta)
    {
        var velocity = _characterBody.Velocity;

        var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        _direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        // Handle sprint
        _isSprinting = Input.IsActionPressed("sprint");
        _currentSpeed = _isSprinting ? SprintSpeed : MoveSpeed;

        // Rotate direction vector relative to camera
        var rotatedDirection = _orientation.GlobalTransform.Basis * _direction;

        // Calculate horizontal and vertical components separately
        var horizontalVelocity = new Vector3(velocity.X, 0, velocity.Z);
        var verticalVelocity = velocity.Y;

        // Target horizontal velocity (air control doesn't affect vertical movement)
        var targetHorizontalVelocity = rotatedDirection * _currentSpeed;
        var targetVerticalVelocity = verticalVelocity;

        // Apply gravity (always full strength, unaffected by air control)
        if (!_characterBody.IsOnFloor())
            targetVerticalVelocity -= _gravity * delta;

        // Calculate acceleration rates
        var accel = _characterBody.IsOnFloor() ? Acceleration : Acceleration * AirControl;
        var decel = _characterBody.IsOnFloor() ? Deceleration : Deceleration * AirControl;

        // Only apply air control to horizontal movement
        horizontalVelocity = horizontalVelocity.Lerp(
            targetHorizontalVelocity,
            (rotatedDirection.LengthSquared() > 0 ? accel : decel) * delta
        );

        // Combine components back into final velocity
        velocity = new Vector3(horizontalVelocity.X, targetVerticalVelocity, horizontalVelocity.Z);

        _characterBody.MoveAndSlide();

        // Jump handling
        if (_jumpBufferTimer > 0 && CanJump())
        {
            _characterBody.Velocity = new Vector3(velocity.X, JumpVelocity, velocity.Z);
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
        var verticalInput = 0;
        if (Input.IsActionPressed("jump"))
            verticalInput = 1;
        if (Input.IsActionPressed("sneak"))
            verticalInput = -1;

        // Rotate direction vector relative to camera
        var rotatedDirection = _orientation.GlobalTransform.Basis * _direction;
        rotatedDirection.Y = verticalInput;

        var targetVelocity = rotatedDirection * (_isSprinting ? FlyingSprintSpeed : FlyingSpeed);

        // Interpolate velocity
        _characterBody.Velocity = _characterBody.Velocity.Lerp(
            targetVelocity,
            FlyingAcceleration * delta
        );

        _characterBody.MoveAndSlide();
    }

    private bool CanJump() => _characterBody.IsOnFloor() || _coyoteTimer > 0;

    private void HandleCoyoteTime(double delta)
    {
        if (_characterBody.IsOnFloor())
            _coyoteTimer = CoyoteTime;
        else
            _coyoteTimer -= delta;
    }

    private void HandleJumpBuffer(double delta)
    {
        if (Input.IsActionJustPressed("jump"))
        {
            // Check for double tap
            var currentTime = Time.GetTicksMsec() / 1000;
            if (currentTime - _lastJumpPressTime < DoubleTapThreshold)
            {
                _isFlying = !_isFlying;
                var v = _characterBody.Velocity;
                if (_isFlying)
                    _characterBody.Velocity = new Vector3(v.X, 0, v.Z);
            }

            _lastJumpPressTime = currentTime;

            _jumpBufferTimer = JumpBufferTime;
        }
        else if (_jumpBufferTimer > 0)
        {
            _jumpBufferTimer -= delta;
        }
    }

    private void HandleCameraTilt(double delta)
    {
        double targetTilt = 0;

        if (Input.IsActionPressed("move_left"))
            targetTilt += CameraTiltAmount;
        if (Input.IsActionPressed("move_right"))
            targetTilt -= CameraTiltAmount;

        _cameraTilt = Mathf.Lerp(_cameraTilt, targetTilt, CameraTiltSpeed * delta);
        _camera.Rotation = new Vector3(
            _camera.Rotation.X,
            _camera.Rotation.Y,
            Mathf.DegToRad(_cameraTilt)
        );
    }
}
