namespace Math;

public static class Math
{
    public static float Abs(float value) => value >= 0f ? value : -value;
    public static double Min(double a, double b) => a < b ? a : b;
    public static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);
    public static float RadiansToDegrees(float radians) => radians * (180f / MathF.PI);
    public static float Sqrt(float value) => MathF.Sqrt(value);

    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        return value > max ? max : value;
    }
}