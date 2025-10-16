public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] PixelData { get; set; }

    public byte[] ToBytes()
    {
        var result = new byte[8 + PixelData.Length];
        BitConverter.GetBytes(Width).CopyTo(result, 0);
        BitConverter.GetBytes(Height).CopyTo(result, 4);
        PixelData.CopyTo(result, 8);
        return result;
    }

    public static FrameData FromBytes(byte[] data)
    {
        return new FrameData
        {
            Width = BitConverter.ToInt32(data, 0),
            Height = BitConverter.ToInt32(data, 4),
            PixelData = new ArraySegment<byte>(data, 8, data.Length - 8).ToArray()
        };
    }
}