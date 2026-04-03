using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using InputSystem;
using SceneSystem.EngineObject.GameObject;

public class Player : MonoBehaviour
{
    private readonly float _movementSpeed = 8f;
    private readonly float _mouseSensitivity = 0.15f;

    private float _pitch = 0f;
    private float _yaw = 0f;

    private Vector3 _direction = Vector3.Zero;

    private Transform _cameraTransform;
    private float _deltaX, _deltaY;

    public void OnEnable()
    {
        _cameraTransform = GetComponent<Camera>().transform;
    }

    public void Update()
    {
        ReadMouse();
        RotateCamera();
        ReadMovementInput();
        MoveCamera();

        Input.ResetMouse();
    }

    private void ReadMouse()
    {
        (_deltaX, _deltaY) = Input.Delta;
        const float deadZone = 0.0001f;
        if (Math.Math.Abs(_deltaX) < deadZone) _deltaX = 0f;
        if (Math.Math.Abs(_deltaY) < deadZone) _deltaY = 0f;
    }

    private void ReadMovementInput()
    {
        _direction = Vector3.Zero;

        if (Input.IsKeyDown(KeyCode.W)) _direction += _cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.S)) _direction -= _cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.A)) _direction -= _cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.D)) _direction += _cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.Space)) _direction += _cameraTransform.Up;
        if (Input.IsKeyDown(KeyCode.LeftShift)) _direction -= _cameraTransform.Up;
    }

    private void MoveCamera()
    {
        if (_direction.LengthSquared > 0f)
        {
            _direction = _direction.Normalized();
            _cameraTransform.LocalPosition += _direction * _movementSpeed * Time.DeltaTime;
        }
    }

    private void RotateCamera()
    {
        if (_deltaX == 0f && _deltaY == 0f)
            return;

        _yaw -= _deltaX * _mouseSensitivity;
        _pitch -= _deltaY * _mouseSensitivity;

        _pitch = Math.Math.Clamp(_pitch, -89f, 89f);

        float pitchRad = Math.Math.DegreesToRadians(_pitch);
        float yawRad = Math.Math.DegreesToRadians(_yaw);

        var qYaw = Quaternion.FromAxisAngle(new Vector3(0f, 1f, 0f), yawRad);
        var qPitch = Quaternion.FromAxisAngle(new Vector3(1f, 0f, 0f), pitchRad);

        _cameraTransform.LocalRotationQuat = qYaw * qPitch;

        var euler = _cameraTransform.LocalRotationQuat.ToEulerAngles();
        _cameraTransform.LocalRotation = new Vector3(
            Math.Math.RadiansToDegrees(euler.X),
            Math.Math.RadiansToDegrees(euler.Y),
            Math.Math.RadiansToDegrees(euler.Z)
        );
    }
}