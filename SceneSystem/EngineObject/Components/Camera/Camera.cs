namespace DustyEngine.Components
{
    public class Camera : CameraBase
    {
        private Transform SceneTransform => GameObject.GetComponent<Transform>();
        protected override Transform TransformSource => SceneTransform;
    }
}