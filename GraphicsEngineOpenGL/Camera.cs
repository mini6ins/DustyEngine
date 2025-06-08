using System.Text.Json.Serialization;
using DustyEngine;
using DustyEngine.Components;
using OpenTK.Mathematics;

namespace GraphicsEngineOpenGL;

public class Camera : MonoBehaviour
{
    private Transform transform;
    
    [JsonIgnore] public Vector3 Front => CalculateFrontFromRotation();
    [JsonIgnore] public Vector3 Up => Vector3.UnitY;
    [JsonIgnore] public Vector3 Right => Vector3.Normalize(Vector3.Cross(Front, Up));

    private void OnEnable()
    {
        transform = GetComponent<Transform>() ?? throw new Exception("Camera requires a Transform component.");
    }
    
    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(transform.LocalPosition.ToOpenTK(), transform.LocalPosition.ToOpenTK() + Front, Up);
    }

    public void UpdateRotation(float deltaYaw, float deltaPitch)
    {
        var rotation = transform.LocalRotation; 
        rotation.Y += deltaYaw;
        rotation.X -= deltaPitch;
        rotation.X = Math.Math.Clamp(rotation.X, -89f, 89f);
        transform.LocalRotation = rotation;
    }

    private Vector3 CalculateFrontFromRotation()
    {
        var rotation = transform.LocalRotation;
        float yaw = MathHelper.DegreesToRadians(rotation.Y - 90);
        float pitch = MathHelper.DegreesToRadians(rotation.X);

        Vector3 front;
        front.X = MathF.Cos(yaw) * MathF.Cos(pitch);   // влево/вправо
        front.Y = MathF.Sin(pitch);                     // вверх/вниз
        front.Z = MathF.Sin(yaw) * MathF.Cos(pitch);   // вперед/назад


        
        return Vector3.Normalize(front);
    }
}