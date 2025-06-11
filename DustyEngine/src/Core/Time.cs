namespace DustyEngine;

using System.Diagnostics;

public static class Time
{
    public static float DeltaTime { get; private set; }

    private static long lastTimestamp;
    private static double timestampToSeconds;

    public static void Init()
    {
        timestampToSeconds = 1.0 / Stopwatch.Frequency;
        lastTimestamp = Stopwatch.GetTimestamp();
        DeltaTime = 0f;
    }
    
    public static void Tick()
    {
        var now = Stopwatch.GetTimestamp();
        
        DeltaTime = (float)((now - lastTimestamp) * timestampToSeconds);
        lastTimestamp = now;
    }
}
