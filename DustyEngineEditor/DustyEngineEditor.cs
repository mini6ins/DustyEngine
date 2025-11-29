using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using StreamJsonRpc;


class Runner
{
    private static void Main(string[] args) => new DustyEngineEditor().RunEditor();
}

class DustyEngineEditor
{
    private const string ProjectPath = "/home/maksym/DustyEngine/TestProject";
    private readonly string RunnerPath = "/home/maksym/DustyEngine/Runner/bin/Debug/net9.0/Runner";

    private Process? _engineProcess;
    private volatile bool _engineRunning;


    public void RunEditor()
    {
        StartEngineProcess();
        ConnectToEngineByStreamJsonRpc();
    }

    private void ConnectToEngineByStreamJsonRpc()
    {
        Console.Title = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);

        var stream = new NamedPipeClientStream(".", "StreamJsonRpcSamplePipe", PipeDirection.InOut,
            PipeOptions.Asynchronous);
        stream.Connect();

        var jsonRpc = JsonRpc.Attach(stream);
        var renderer = jsonRpc.Attach<IRemoteRenderer>();
        using (RenderWindow clientWindow = new RenderWindow(renderer))
        {
            clientWindow.Run();
        }

        stream.Dispose();
    }

    private void StartEngineProcess()
    {
        if (_engineRunning) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = RunnerPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add(ProjectPath);
            psi.ArgumentList.Add("Editor");
            // psi.ArgumentList.Add("Standalone");


            _engineProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            _engineProcess.Exited += (_, __) => { _engineRunning = false; };

            _engineProcess.Start();
            _engineProcess.BeginOutputReadLine();
            _engineProcess.BeginErrorReadLine();

            _engineRunning = true;
        }
        catch (Exception ex)
        {
            _engineRunning = false;
        }
    }
}

public interface IRemoteRenderer
{
    Task<FrameData> GetFrameData(float time);
    void OnKeyDown(string key);
    void OnKeyUp(string key);
    void OnMouseDown(float normalizedX, float normalizedY, int button);
    void OnMouseUp(float normalizedX, float normalizedY, int button);
    void OnMouseMoveDelta(float deltaX, float deltaY); 
    void OnMouseClick(float normalizedX, float normalizedY, int button);
}

public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public float Timestamp { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}