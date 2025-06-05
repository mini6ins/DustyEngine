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

    public override string ToString() => $"({X}, {Y}, {Z})";
}