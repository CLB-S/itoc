
using System;
using Godot;

partial class FreeLookCameraBase : Camera3D
{
    // This is a translation from the original free camera found in AssetStore 

    // Modifier keys' speed multiplier
    const float SHIFT_MULTIPLIER = 2.5f;
    const float ALT_MULTIPLIER = 1.0f / SHIFT_MULTIPLIER;

    [Export(PropertyHint.Range, "0.0f,1.0f")]
    public float sensitivity = 0.25f;

    // Mouse state
    private Vector2 _mouse_position = new Vector2(0.0f, 0.0f);
    private float _total_pitch = 0.0f;

    // Movement state
    private Vector3 _direction = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 _velocity = new Vector3(0.0f, 0.0f, 0.0f);
    private float _acceleration = 30f;
    private float _deceleration = -10f;
    private float _vel_multiplier = 4f;

    // Keyboard state
    private bool _w = false;
    private bool _s = false;
    private bool _a = false;
    private bool _d = false;
    private bool _q = false;
    private bool _e = false;
    private bool _shift = false;
    private bool _alt = false;

    public override void _Input(InputEvent _event)
    {
        // Receives mouse motion
        InputEventMouseMotion mouseMotionEvent = _event as InputEventMouseMotion;
        if (mouseMotionEvent != null)
        {
            _mouse_position = mouseMotionEvent.Relative;
        }

        // Receives mouse button input
        InputEventMouseButton mouseButtonEvent = _event as InputEventMouseButton;
        if (mouseButtonEvent != null)
        {
            switch (mouseButtonEvent.ButtonIndex)
            {
                case MouseButton.Right: // Only allows rotation if right click down
                    {
                        Input.MouseMode = mouseButtonEvent.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                    }
                    break;

                case MouseButton.WheelUp: // Increases max velocity
                    {
                        _vel_multiplier = Mathf.Clamp(_vel_multiplier * 1.1f, 0.2f, 20f);
                    }
                    break;

                case MouseButton.WheelDown: // Decreases max velocity
                    {
                        _vel_multiplier = Mathf.Clamp(_vel_multiplier / 1.1f, 0.2f, 20f);
                    }
                    break;
            }
        }

        // Receives key input
        InputEventKey keyEvent = _event as InputEventKey;
        if (keyEvent != null)
        {
            switch (keyEvent.Keycode)
            {
                case Key.W:
                    {
                        _w = keyEvent.Pressed;
                    }
                    break;

                case Key.S:
                    {
                        _s = keyEvent.Pressed;
                    }
                    break;

                case Key.A:
                    {
                        _a = keyEvent.Pressed;
                    }
                    break;

                case Key.D:
                    {
                        _d = keyEvent.Pressed;
                    }
                    break;

                case Key.Q:
                    {
                        _q = keyEvent.Pressed;
                    }
                    break;

                case Key.E:
                    {
                        _e = keyEvent.Pressed;
                    }
                    break;
                case Key.Shift:
                    {
                        _shift = keyEvent.Pressed;
                    }
                    break;
                case Key.Alt:
                    {
                        _alt = keyEvent.Pressed;
                    }
                    break;
            }
        }
    }

    // Updates mouselook and movement every frame
    public override void _Process(double delta)
    {
        _update_mouselook();
        _update_movement((float)delta);
    }

    // Updates camera movement
    private void _update_movement(float delta)
    {
        // Computes desired direction from key states
        _direction = Vector3.Zero;
        if (_d) _direction.X += 1.0f;
        if (_a) _direction.X -= 1.0f;
        if (_e) _direction.Y += 1.0f;
        if (_q) _direction.Y -= 1.0f;
        if (_s) _direction.Z += 1.0f;
        if (_w) _direction.Z -= 1.0f;

        // Computes the change in velocity due to desired direction and "drag"
        // The "drag" is a constant acceleration on the camera to bring it's velocity to 0
        Vector3 offset = _direction.Normalized() * _acceleration * _vel_multiplier * delta
                       + _velocity.Normalized() * _deceleration * _vel_multiplier * delta;

        // Compute modifiers' speed multiplier
        float speed_multi = 1.0f;
        if (_shift) speed_multi *= SHIFT_MULTIPLIER;
        if (_alt) speed_multi *= ALT_MULTIPLIER;

        // Checks if we should bother translating the camera
        if ((_direction == Vector3.Zero) && (offset.LengthSquared() > _velocity.LengthSquared()))
        {
            // Sets the velocity to 0 to prevent jittering due to imperfect deceleration
            _velocity = Vector3.Zero;
        }
        else
        {
            // Clamps speed to stay within maximum value (_vel_multiplier)
            _velocity.X = Mathf.Clamp(_velocity.X + offset.X, -_vel_multiplier, _vel_multiplier);
            _velocity.Y = Mathf.Clamp(_velocity.Y + offset.Y, -_vel_multiplier, _vel_multiplier);
            _velocity.Z = Mathf.Clamp(_velocity.Z + offset.Z, -_vel_multiplier, _vel_multiplier);

            Translate(_velocity * delta * speed_multi);
        }
    }

    // Updates mouse look
    private void _update_mouselook()
    {
        // Only rotates mouse if the mouse is 
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _mouse_position *= sensitivity;
            float yaw = _mouse_position.X;
            float pitch = _mouse_position.Y;
            _mouse_position = Vector2.Zero;

            // Prevents looking up/down too far
            pitch = Mathf.Clamp(pitch, -90 - _total_pitch, 90 - _total_pitch);
            _total_pitch += pitch;

            RotateY(Mathf.DegToRad(-yaw));
            RotateObjectLocal(new Vector3(1.0f, 0.0f, 0.0f), Mathf.DegToRad(-pitch));
        }
    }
}
