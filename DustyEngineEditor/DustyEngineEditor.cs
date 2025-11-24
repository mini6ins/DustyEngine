using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading.Tasks;
using StreamJsonRpc;

class Client
{
    private static void Main(string[] args)
    {
        Console.Title = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);

        Console.WriteLine("Connecting to server...");

        var stream = new NamedPipeClientStream(".", "StreamJsonRpcSamplePipe",
            PipeDirection.InOut, PipeOptions.Asynchronous);

        stream.Connect();
        Console.WriteLine("Connected. Starting render client...");

        var jsonRpc = JsonRpc.Attach(stream);
        var renderer = jsonRpc.Attach<IRemoteRenderer>();

        // НЕ МЕНЯЕТСЯ - тот же RenderWindow, просто внутри теперь ImGui!
        using (RenderWindow clientWindow = new RenderWindow(renderer))
        {
            clientWindow.Run();
        }

        stream.Dispose();
        Console.WriteLine("Terminating stream...");
    }
}

public interface IRemoteRenderer
{
    Task<FrameData> GetFrameData(float time);
    void OnKeyPress(string key);
    void OnMouseMove(float normalizedX, float normalizedY);
    void OnMouseClick(float normalizedX, float normalizedY, int button);
}

public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public float Timestamp { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}