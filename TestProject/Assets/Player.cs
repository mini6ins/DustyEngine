using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using InputSystem;
using SceneSystem.EngineObject.GameObject;

public class Player : MonoBehaviour
{
    private float _movementSpeed = 8f;
    private float _mouseSensitivity = 0.15f;

    private float _pitch = 0f;
    private float _yaw = 0f;

    private Vector3 _direction = Vector3.Zero;

    private Transform _cameraTransform;
    private float _deltaX, _deltaY;

    private float _smoothDeltaX = 0f;
    private float _smoothDeltaY = 0f;
    private const float CSmoothing = 0.3f;

    private GameObject _testObject;

    public void OnEnable()
    {
        _cameraTransform = GetComponent<Camera>().transform;
        _testObject = new GameObject("testObject");
        _testObject.AddComponent(new Transform(new Vector3(5f, 5f, 0f)));
        _testObject.AddComponent(new MeshRenderer(null, "/home/maksym/Projects/DustyEngine/TestProject/Assets/cube.obj"));
    }

    private void Update()
    {
        (_deltaX, _deltaY) = Input.Delta;

        _smoothDeltaX = _smoothDeltaX * CSmoothing + _deltaX * (1f - CSmoothing);
        _smoothDeltaY = _smoothDeltaY * CSmoothing + _deltaY * (1f - CSmoothing);

        RotateCamera();
        Input.ResetMouse();

        _direction = Vector3.Zero;
        if (Input.IsKeyDown(KeyCode.W)) _direction += _cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.S)) _direction -= _cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.A)) _direction -= _cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.D)) _direction += _cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.Space)) _direction += _cameraTransform.Up;
        if (Input.IsKeyDown(KeyCode.LeftShift)) _direction -= _cameraTransform.Up;

        if (Input.IsKeyJustActivatedOnce(KeyCode.E)) Instantiate(_testObject);
        if (Input.IsKeyJustActivatedOnce(KeyCode.F)) Destroy(_testObject);

        MoveCamera();
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
        const float deadZone = 0.001f;

        float effectiveDeltaX = _smoothDeltaX;
        float effectiveDeltaY = _smoothDeltaY;

        if (Math.Math.Abs(effectiveDeltaX) < deadZone && Math.Math.Abs(effectiveDeltaY) < deadZone)
            return;

        _yaw -= effectiveDeltaX * _mouseSensitivity;
        _pitch -= effectiveDeltaY * _mouseSensitivity;

        _pitch = Math.Math.Clamp(_pitch, -89f, 89f);

        float pitchRad = Math.Math.DegreesToRadians(_pitch);
        float yawRad = Math.Math.DegreesToRadians(_yaw);

        var currentRight = _cameraTransform.LocalRotationQuat.Rotate(new Vector3(1f, 0f, 0f));
        var qPitch = Quaternion.FromAxisAngle(currentRight, pitchRad);

        var localUp = qPitch.Rotate(new Vector3(0f, 1f, 0f));
        var qYaw = Quaternion.FromAxisAngle(localUp, yawRad);

        _cameraTransform.LocalRotationQuat = qYaw * qPitch;

        var euler = _cameraTransform.LocalRotationQuat.ToEulerAngles();
        _cameraTransform.LocalRotation = new Vector3(
            Math.Math.RadiansToDegrees(euler.X),
            Math.Math.RadiansToDegrees(euler.Y),
            Math.Math.RadiansToDegrees(euler.Z)
        );
    }
}
