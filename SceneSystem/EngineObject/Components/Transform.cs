using System.Text.Json.Serialization;
using OpenTK.Mathematics;
using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace DustyEngine.Components
{
    public class Transform : Component
    {
        public Vector3 LocalPosition
        {
            get => _localPosition;
            set { _localPosition = value; }
        }

        public Vector3 LocalRotation
        {
            get => _localRotation;
            set { _localRotation = value; }
        }

        public Vector3 LocalScale
        {
            get => _localScale;
            set { _localScale = value; }
        }

        [JsonIgnore] private Vector3 _localPosition = new Vector3(0, 0, 0);
        [JsonIgnore] private Vector3 _localRotation = new Vector3(0, 0, 0);
        [JsonIgnore] private Vector3 _localScale = new Vector3(1, 1, 1);

        [JsonIgnore]
        public Vector3 GlobalPosition
        {
            get
            {
                if (Parent.Parent == null)
                    return _localPosition;

                var parentTransform = Parent.Parent.GetComponent<Transform>();
                return parentTransform != null ? parentTransform.GlobalPosition + _localPosition : _localPosition;
            }
        }

        [JsonIgnore]
        public Vector3 GlobalRotation
        {
            get
            {
                if (Parent.Parent == null)
                    return _localRotation;

                var parentTransform = Parent.Parent.GetComponent<Transform>();
                return parentTransform != null ? parentTransform.GlobalRotation + _localRotation : _localRotation;
            }
        }

        [JsonIgnore]
        public Vector3 GlobalScale
        {
            get
            {
                if (Parent.Parent == null)
                    return _localScale;

                var parentTransform = Parent.Parent.GetComponent<Transform>();
                return parentTransform != null
                    ? new Vector3(
                        parentTransform.GlobalScale.X * _localScale.X,
                        parentTransform.GlobalScale.Y * _localScale.Y,
                        parentTransform.GlobalScale.Z * _localScale.Z)
                    : _localScale;
            }
        }

        [JsonIgnore]
        public OpenTK.Mathematics.Vector3 Forward
        {
            get
            {
                var yaw = MathHelper.DegreesToRadians(LocalRotation.Y - 90);
                var pitch = MathHelper.DegreesToRadians(LocalRotation.X);

                OpenTK.Mathematics.Vector3 front;
                front.X = MathF.Cos(yaw) * MathF.Cos(pitch);
                front.Y = MathF.Sin(pitch);
                front.Z = MathF.Sin(yaw) * MathF.Cos(pitch);

                return OpenTK.Mathematics.Vector3.Normalize(front);
            }
        }

        [JsonIgnore]
        public OpenTK.Mathematics.Vector3 Right =>
            OpenTK.Mathematics.Vector3.Normalize(
                OpenTK.Mathematics.Vector3.Cross(Forward, new OpenTK.Mathematics.Vector3(0, 1, 0)));

        [JsonIgnore]
        public OpenTK.Mathematics.Vector3 Up =>
            OpenTK.Mathematics.Vector3.Normalize(OpenTK.Mathematics.Vector3.Cross(Right, Forward));


        public override string ToString()
        {
            return $"Local: {LocalPosition}, Rotation: {LocalRotation}, Scale: {LocalScale}, " +
                   $"Global: {GlobalPosition}, GlobalRotation: {GlobalRotation}, GlobalScale: {GlobalScale}";
        }
    }
}