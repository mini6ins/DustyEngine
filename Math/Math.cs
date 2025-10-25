public static class Math 
{
    public const float Pi = 3.14159265f;

    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
    
    public static float Abs(float value)
    {
        return value >= 0f ? value : -value;
    }

    public static float Max(float a, float b)
    {
        return a > b ? a : b;
    }
    
    public static float Min(float a, float b)
    {
        return a < b ? a : b;
    }
    public static double Min(double a, double b)
    {
        return a < b ? a : b;
    }

    public static float DegreesToRadians(float degrees)
    {
        return degrees * (MathF.PI / 180f);
    }

    public static float RadiansToDegrees(float radians)
    {
        return radians * (180f / MathF.PI);
    }

    public static float Sqrt(float value)
    {
        return MathF.Sqrt(value);
    }
}