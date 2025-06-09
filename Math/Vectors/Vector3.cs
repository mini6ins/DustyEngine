namespace DustyEngine.Engine.Math.Vectors;

public class Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3() : this(0, 0, 0)
    {
    }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) =>
        new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator -(Vector3 a, Vector3 b) =>
        new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 operator *(Vector3 a, Vector3 b) =>
        new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    public static Vector3 operator /(Vector3 a, float scalar) => 
        new Vector3(a.X / scalar, a.Y / scalar, a.Z / scalar);

    public static Vector3 operator *(Vector3 v, float scalar) =>
        new Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static Vector3 operator *(float scalar, Vector3 v) => 
        v * scalar; 
    
    public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);
    public float LengthSquared => X * X + Y * Y + Z * Z;
    
    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    public static Vector3 Normalize(Vector3 v)
    {
        float length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        if (length == 0) return new Vector3(0, 0, 0);
        return new Vector3(v.X / length, v.Y / length, v.Z / length);
    }

    public Vector3 Normalized()
    {
        float length = Length;
        if (length == 0)
            return new Vector3(0, 0, 0);
        return new Vector3(X / length, Y / length, Z / length);
    }

    public OpenTK.Mathematics.Vector3 ToOpenTK() => new (X, Y, Z);
    
    public override string ToString() => $"({X}, {Y}, {Z})";
}