using OpenTK.Mathematics;
using SceneSystem.Attributes;
using V3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace DustyEngine.Components
{
    public class CameraBase : MonoBehaviour
    {
        [SerializeField] private float _fieldOfView = 45.0f;
        [SerializeField] private float _nearPlane = 0.5f;
        [SerializeField] private float _farPlane = 200.0f;

        [ReadOnlyInInspector] public float AspectRatio { get; set; } = 16f / 9f;

        protected virtual Transform TransformSource => InternalTransform;

        [HideInInspector]
        public Transform InternalTransform { get; } = new(
            new V3(0, 2.5f, 5),
            new V3(0, 0, 0),
            new V3(1, 1, 1)
        );

        public virtual Matrix4 GetViewMatrix()
        {
            var t = TransformSource;
            return Matrix4.LookAt(
                t.LocalPosition.ToOpenTK(),
                t.LocalPosition.ToOpenTK() + t.Forward.ToOpenTK(),
                t.Up.ToOpenTK()
            );
        }

        public virtual Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(_fieldOfView),
                AspectRatio,
                _nearPlane,
                _farPlane
            );
        }
    }
}
