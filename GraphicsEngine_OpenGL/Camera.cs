using OpenTK.Mathematics;

namespace GraphicsEngine_OpenGL;

public class Camera
{
    public Vector3 Position { get; private set; }
    public Vector3 Front { get; private set; } = -Vector3.UnitZ;
    public Vector3 Up { get; private set; } = Vector3.UnitY;
    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Front, Up));

    public float Yaw { get; private set; } = -90f;
    public float Pitch { get; private set; } = 0f;

    public Camera(Vector3 startPosition, float yaw = -90f, float pitch = 0f)
    {
        Position = startPosition;
        Yaw = yaw;
        Pitch = pitch;
        UpdateVectors();
    }
    
    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Front, Up);
    }
    
    public void UpdateRotation(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch -= deltaPitch;
        Pitch = Clamp(Pitch, -89f, 89f);
        UpdateVectors();
    }
    
    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    private void UpdateVectors()
    {
        Vector3 front;
        front.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
        front.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
        front.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
        Front = Vector3.Normalize(front);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
} 
