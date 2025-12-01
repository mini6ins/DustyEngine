using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace DustyEngine.Components;

public class EditorCamera : CameraBase
{
    private float _yaw;
    private float _pitch;
    private float _smoothDx;
    private float _smoothDy;
    private const float Smoothing = 0.7f;

    private const float Speed = 8f;

    private const float MouseSensitivity = 0.15f;

    public void InitializeController()
    {
        var euler = InternalTransform.LocalRotationQuat.ToEulerAngles();
        _pitch = euler.X * (180f / MathF.PI);
        _yaw = euler.Y * (180f / MathF.PI);
    }

    public void UpdateMovement(float deltaTime, bool isMiddleMouseDown, (float dx, float dy) mouseDelta,
        MovementInput movementInput)
    {
        HandleRotation(isMiddleMouseDown, mouseDelta);
        HandleMovement(deltaTime, movementInput);
    }

    private void HandleMovement(float deltaTime, MovementInput input)
    {
        var fwd = InternalTransform.Forward;
        var right = InternalTransform.Right;
        var up = InternalTransform.Up;
        var dir = Vector3.Zero;

        if (input.Forward) dir += fwd;
        if (input.Backward) dir -= fwd;
        if (input.Left) dir -= right;
        if (input.Right) dir += right;
        if (input.Up) dir += up;
        if (input.Down) dir -= up;

        if (!(dir.LengthSquared > 0f)) return;
        
        dir = dir.Normalized();
        InternalTransform.LocalPosition += dir * Speed * deltaTime;
    }

    private void HandleRotation(bool shouldRotate, (float dx, float dy) mouseDelta)
    {
        if (shouldRotate)
        {
            var (dx, dy) = mouseDelta;

            _smoothDx = _smoothDx * Smoothing + dx * (1f - Smoothing);
            _smoothDy = _smoothDy * Smoothing + dy * (1f - Smoothing);

            const float deadZone = 0.0001f;
            if (System.Math.Abs(_smoothDx) > deadZone || System.Math.Abs(_smoothDy) > deadZone)
            {
                _yaw -= _smoothDx * MouseSensitivity;
                _pitch -= _smoothDy * MouseSensitivity;
                _pitch = System.Math.Clamp(_pitch, -89f, 89f);

                UpdateCameraRotation();
            }
        }
        else
        {
            _smoothDx *= Smoothing;
            _smoothDy *= Smoothing;
        }
    }

    private void UpdateCameraRotation()
    {
        var pitchRad = _pitch * (MathF.PI / 180f);
        var yawRad = _yaw * (MathF.PI / 180f);

        var currentRight = InternalTransform.LocalRotationQuat.Rotate(new Vector3(1f, 0f, 0f));
        var qPitch = Quaternion.FromAxisAngle(currentRight, pitchRad);
        var localUp = qPitch.Rotate(new Vector3(0f, 1f, 0f));
        var qYaw = Quaternion.FromAxisAngle(localUp, yawRad);

        InternalTransform.LocalRotationQuat = qYaw * qPitch;
    }
}

public struct MovementInput
{
    public bool Forward;
    public bool Backward;
    public bool Left;
    public bool Right;
    public bool Up;
    public bool Down;
}