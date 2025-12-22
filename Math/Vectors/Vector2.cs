namespace DustyEngine.Engine.Math.Vectors;

public struct Vector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2() : this(0, 0) { }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) =>
        new Vector2(a.X + b.X, a.Y + b.Y);

    public static Vector2 operator -(Vector2 a, Vector2 b) =>
        new Vector2(a.X - b.X, a.Y - b.Y);

    public static Vector2 operator *(Vector2 a, Vector2 b) =>
        new Vector2(a.X * b.X, a.Y * b.Y);

    public static Vector2 operator *(Vector2 a, float scalar) =>
        new Vector2(a.X * scalar, a.Y * scalar);

    public static Vector2 operator /(Vector2 a, float scalar) =>
        new Vector2(a.X / scalar, a.Y / scalar);

    public static Vector2 operator +(Vector2 a, OpenTK.Mathematics.Vector2 b) =>
        new Vector2(a.X + b.X, a.Y + b.Y);

    public static Vector2 operator -(Vector2 a, OpenTK.Mathematics.Vector2 b) =>
        new Vector2(a.X - b.X, a.Y - b.Y);

    public static Vector2 operator *(Vector2 a, OpenTK.Mathematics.Vector2 b) =>
        new Vector2(a.X * b.X, a.Y * b.Y);

    public OpenTK.Mathematics.Vector2 ToOpenTK() => new OpenTK.Mathematics.Vector2(X, Y);

    public static Vector2 FromOpenTK(OpenTK.Mathematics.Vector2 v) =>
        new Vector2(v.X, v.Y);

    public override string ToString() => $"({X}, {Y})";
}

public class Vector2i
{
    public int X { get; set; }
    public int Y { get; set; }

    public Vector2i() : this(0, 0) { }

    public Vector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Vector2i operator +(Vector2i a, Vector2i b) =>
        new Vector2i(a.X + b.X, a.Y + b.Y);

    public static Vector2i operator -(Vector2i a, Vector2i b) =>
        new Vector2i(a.X - b.X, a.Y - b.Y);

    public static Vector2i operator *(Vector2i a, Vector2i b) =>
        new Vector2i(a.X * b.X, a.Y * b.Y);

    public static Vector2i operator *(Vector2i a, int scalar) =>
        new Vector2i(a.X * scalar, a.Y * scalar);

    public static Vector2i operator /(Vector2i a, int scalar) =>
        new Vector2i(a.X / scalar, a.Y / scalar);

    public static Vector2i operator +(Vector2i a, OpenTK.Mathematics.Vector2i b) =>
        new Vector2i(a.X + b.X, a.Y + b.Y);

    public static Vector2i operator -(Vector2i a, OpenTK.Mathematics.Vector2i b) =>
        new Vector2i(a.X - b.X, a.Y - b.Y);

    public static Vector2i operator *(Vector2i a, OpenTK.Mathematics.Vector2i b) =>
        new Vector2i(a.X * b.X, a.Y * b.Y);

    public OpenTK.Mathematics.Vector2i ToOpenTK() => new OpenTK.Mathematics.Vector2i(X, Y);

    public static Vector2i FromOpenTK(OpenTK.Mathematics.Vector2i v) =>
        new Vector2i(v.X, v.Y);

    public override string ToString() => $"({X}, {Y})";
}
