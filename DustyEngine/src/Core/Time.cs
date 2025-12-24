using System.Diagnostics;

namespace DustyEngine;

public static class Time
{
    public static float DeltaTime { get; private set; }

    private static long _lastTimestamp;
    private static double _timestampToSeconds;

    public static void Init()
    {
        Reset();
        _timestampToSeconds = 1.0 / Stopwatch.Frequency;
    }

    public static void Tick()
    {
        var now = Stopwatch.GetTimestamp();

        DeltaTime = (float)((now - _lastTimestamp) * _timestampToSeconds);
        _lastTimestamp = now;
    }

    public static void Reset()
    {
        _lastTimestamp = Stopwatch.GetTimestamp();
        DeltaTime = 0f;
    }
}
