using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using Utils;

public class Player : MonoBehaviour
{
    private float movementSpeed = 8f;
    private float mouseSensitivity = 0.15f; // ✅ Немного снижена чувствительность

    private float pitch = 0f;
    private float yaw = 0f;

    private Vector3 direction = Vector3.Zero;
    private Vector3 velocity = Vector3.Zero;

    private Transform cameraTransform;
    private float deltaX, deltaY;

    // ✅ Сглаживание мыши (опционально)
    private float smoothDeltaX = 0f;
    private float smoothDeltaY = 0f;
    private const float SMOOTHING = 0.3f; // 0 = без сглаживания, 1 = максимальное

    private GameObject testObject;

    public void OnEnable()
    {
        cameraTransform = GetComponent<Camera>().transform;
        testObject = new GameObject("testObject");
        testObject.AddComponent(new Transform(new Vector3(0f, 5f, 0f)));
        testObject.AddComponent(new MeshRenderer(null,
            "/home/maksym/github/DustyEngine/TestProject/Assets/TeddyBear.obj"));
    }

    private void Update()
    {
        (deltaX, deltaY) = Input.Delta;

        // ✅ Применяем сглаживание (опционально - можно отключить)
        smoothDeltaX = smoothDeltaX * SMOOTHING + deltaX * (1f - SMOOTHING);
        smoothDeltaY = smoothDeltaY * SMOOTHING + deltaY * (1f - SMOOTHING);

        RotateCamera();
        
        // ✅ Сброс после использования
        Input.ResetMouse();

        direction = Vector3.Zero;
        if (Input.IsKeyDown(KeyCode.W)) direction += cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.S)) direction -= cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.A)) direction -= cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.D)) direction += cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.Space)) direction += cameraTransform.Up;
        if (Input.IsKeyDown(KeyCode.LeftShift)) direction -= cameraTransform.Up;

        if (Input.IsKeyJustActivatedOnce(KeyCode.E)) Instantiate(testObject);
        if (Input.IsKeyJustActivatedOnce(KeyCode.F)) Destroy(testObject);

        MoveCamera();
    }

    private void MoveCamera()
    {
        if (direction.LengthSquared > 0f)
        {
            direction = direction.Normalized();
            cameraTransform.LocalPosition += direction * movementSpeed * Time.DeltaTime;
        }
    }

    private void RotateCamera()
    {
        // ✅ Уменьшена dead zone
        const float deadZone = 0.001f;
        
        // ✅ Используем сглаженные значения (или обычные - закомментируйте строки ниже)
        float effectiveDeltaX = smoothDeltaX;
        float effectiveDeltaY = smoothDeltaY;
        
        // Или без сглаживания:
        // float effectiveDeltaX = deltaX;
        // float effectiveDeltaY = deltaY;
        
        if (Math.Abs(effectiveDeltaX) < deadZone && Math.Abs(effectiveDeltaY) < deadZone)
            return;

        yaw -= effectiveDeltaX * mouseSensitivity;
        pitch -= effectiveDeltaY * mouseSensitivity;

        pitch = Math.Clamp(pitch, -89f, 89f);

        float pitchRad = Math.DegreesToRadians(pitch);
        float yawRad = Math.DegreesToRadians(yaw);

        var currentRight = cameraTransform.LocalRotationQuat.Rotate(new Vector3(1f, 0f, 0f));
        var qPitch = Quaternion.FromAxisAngle(currentRight, pitchRad);

        var localUp = qPitch.Rotate(new Vector3(0f, 1f, 0f));
        var qYaw = Quaternion.FromAxisAngle(localUp, yawRad);

        cameraTransform.LocalRotationQuat = qYaw * qPitch;

        var euler = cameraTransform.LocalRotationQuat.ToEulerAngles();
        cameraTransform.LocalRotation = new Vector3(
            Math.RadiansToDegrees(euler.X),
            Math.RadiansToDegrees(euler.Y),
            Math.RadiansToDegrees(euler.Z)
        );
    }
}