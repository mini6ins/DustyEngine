using DustyEngine.Engine.Math.Vectors;

public struct Quaternion
{
    public float X, Y, Z, W;

    public Quaternion(float x, float y, float z, float w)
        => (X, Y, Z, W) = (x, y, z, w);

    public static Quaternion FromAxisAngle(Vector3 axis, float angleRad)
    {
        var n = axis.Normalized();
        float half = angleRad * 0.5f;
        
        float s = MathF.Sin(half);
        float c = MathF.Cos(half);
        return new Quaternion(
            n.X * s,
            n.Y * s,
            n.Z * s,
            c
        );
    }
    
    public Vector3 ToEulerAngles()
    {
        float sinr_cosp = 2f * (W * X + Y * Z);
        float cosr_cosp = 1f - 2f * (X * X + Y * Y);
        float pitch = MathF.Atan2(sinr_cosp, cosr_cosp);

        float sinp = 2f * (W * Y - Z * X);
        float yaw;
        if (MathF.Abs(sinp) >= 1f)
            yaw = MathF.CopySign(MathF.PI / 2f, sinp); 
        else
            yaw = MathF.Asin(sinp);

        float siny_cosp = 2f * (W * Z + X * Y);
        float cosy_cosp = 1f - 2f * (Y * Y + Z * Z);
        float roll = MathF.Atan2(siny_cosp, cosy_cosp);

        return new Vector3(pitch, yaw, roll);
    }
    
    public static Quaternion FromEuler(float pitch, float yaw, float roll)
    {
        float cy = MathF.Cos(yaw * 0.5f);
        float sy = MathF.Sin(yaw * 0.5f);
        float cp = MathF.Cos(pitch * 0.5f);
        float sp = MathF.Sin(pitch * 0.5f);
        float cr = MathF.Cos(roll * 0.5f);
        float sr = MathF.Sin(roll * 0.5f);

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

    public Quaternion Inverted()
        => new Quaternion(-X, -Y, -Z, W);

    public Vector3 Rotate(Vector3 v)
    {
        var qv = new Quaternion(v.X, v.Y, v.Z, 0f);
        var res = this * qv * this.Inverted();
        return new Vector3(res.X, res.Y, res.Z);
    }
}