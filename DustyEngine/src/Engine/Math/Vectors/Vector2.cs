using System;
using OpenTK.Mathematics; // Или просто OpenTK, в зависимости от версии

public class Vector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2() : this(0, 0)
    {
    }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    // --- Операторы с другим Vector2 ---
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