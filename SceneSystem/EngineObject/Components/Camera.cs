using OpenTK.Mathematics;
using SceneSystem.Attributes;

namespace DustyEngine.Components;

public class Camera : MonoBehaviour
{
    [SerializeField] private float _fieldOfView = 45.0f;
    [SerializeField] private float _nearPlane = 0.1f;
    [SerializeField] private float _farPlane = 10000.0f;

    public float AspectRatio { get; set; } = 16f / 9f;
    private Transform _transform => GameObject.GetComponent<Transform>();

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(_transform.LocalPosition.ToOpenTK(),
            _transform.LocalPosition.ToOpenTK() + _transform.Forward.ToOpenTK(), _transform.Up.ToOpenTK());
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(_fieldOfView),
            AspectRatio,
            _nearPlane,
            _farPlane
        );
    }
}