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

    public static Vector3 Zero = new Vector3(0, 0, 0);
    public static Vector3 Up = new Vector3(0, 1, 0);
    public static Vector3 One = new Vector3(1, 1, 1);

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

    public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    
    public static Vector3 ClampMagnitude(Vector3 v, float maxLength)
    {
        var sqrMag = v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        if (sqrMag > maxLength * maxLength)
        {
            var mag = MathF.Sqrt(sqrMag);
            var factor = maxLength / mag;
            return new Vector3(v.X * factor, v.Y * factor, v.Z * factor);
        }

        return v;
    }

    public static Vector3 SmoothDamp(
        Vector3 current,
        Vector3 target,
        ref Vector3 currentVelocity,
        float smoothTime,
        float maxSpeed,
        float deltaTime
    )
    {
        smoothTime = MathF.Max(0.0001f, smoothTime);

        float omega = 2f / smoothTime;
        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        var change = current - target;
        var originalTo = target;
        var maxChange = maxSpeed * smoothTime;
        change = ClampMagnitude(change, maxChange);
        target = current - change;

        var temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;

        var output = target + (change + temp) * exp;

        var origMinusCurrent = originalTo - current;
        var outMinusOrig = output - originalTo;
        if (Dot(origMinusCurrent, outMinusOrig) > 0)
        {
            output = originalTo;
            currentVelocity = (output - originalTo) / deltaTime;
        }

        return output;
    }

    public OpenTK.Mathematics.Vector3 ToOpenTK() => new(X, Y, Z);

    public override string ToString() => $"({X}, {Y}, {Z})";
}