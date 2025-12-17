using DustyEngine;

namespace GraphicsEngineOpenGL;

public class FrameSlot
{
    public FrameData Frame = new();
    public volatile bool IsReady;
}

public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public float Timestamp { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}

public class RpcCallbacks
{
    public Action<string, bool>? OnKeyEvent { get; set; }
    public Action<float, float>? OnMouseMove { get; set; }
    public Action<float, float, int, bool>? OnMouseEvent { get; set; }
    public Action? OnPlayEngine { get; set; }
    public Action? OnStopEngine { get; set; }

    public Action<object, Debug.LogLevel, bool, string, string, string, int>? OnLogMessage { get; init; }
}
