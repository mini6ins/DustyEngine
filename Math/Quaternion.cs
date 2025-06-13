using DustyEngine.Engine.Math.Vectors;

public struct Quaternion(float x, float y, float z, float w)
{
    public float X = x, Y = y, Z = z, W = w;

    public static Quaternion FromAxisAngle(Vector3 axis, float angleRad)
    {
        var n = axis.Normalized();
        var half = angleRad * 0.5f;

        var s = MathF.Sin(half);
        var c = MathF.Cos(half);
        return new Quaternion(
            n.X * s,
            n.Y * s,
            n.Z * s,
            c
        );
    }

    public Vector3 ToEulerAngles()
    {
        var sinr_cosp = 2f * (W * X + Y * Z);
        var cosr_cosp = 1f - 2f * (X * X + Y * Y);
        var pitch = MathF.Atan2(sinr_cosp, cosr_cosp);

        var sinp = 2f * (W * Y - Z * X);
        var yaw = MathF.Abs(sinp) >= 1f ? MathF.CopySign(MathF.PI / 2f, sinp) : MathF.Asin(sinp);

        var siny_cosp = 2f * (W * Z + X * Y);
        var cosy_cosp = 1f - 2f * (Y * Y + Z * Z);
        var roll = MathF.Atan2(siny_cosp, cosy_cosp);

        return new Vector3(pitch, yaw, roll);
    }

    public static Quaternion FromEuler(float pitch, float yaw, float roll)
    {
        var cy = MathF.Cos(yaw * 0.5f);
        var sy = MathF.Sin(yaw * 0.5f);
        var cp = MathF.Cos(pitch * 0.5f);
        var sp = MathF.Sin(pitch * 0.5f);
        var cr = MathF.Cos(roll * 0.5f);
        var sr = MathF.Sin(roll * 0.5f);

        return new Quaternion(
            sr * cp * cy - cr * sp * sy, // X
            cr * sp * cy + sr * cp * sy, // Y
            cr * cp * sy - sr * sp * cy, // Z
            cr * cp * cy + sr * sp * sy // W
        );
    }

    public static Quaternion operator *(Quaternion a, Quaternion b)
        => new Quaternion(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
        );

    public Quaternion Inverted() => new Quaternion(-X, -Y, -Z, W);

    public Vector3 Rotate(Vector3 v)
    {
        var qv = new Quaternion(v.X, v.Y, v.Z, 0f);
        var res = this * qv * this.Inverted();
        return new Vector3(res.X, res.Y, res.Z);
    }
}