namespace DustyEngineEditor.Panels.RemoteRenderer;

public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public float Timestamp { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}