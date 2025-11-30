using System.Diagnostics;
using System.IO.Pipes;
using DustyEngineEditor.Panels;
using DustyEngineEditor.Panels.ViewPortPanel.RemoteRenderer;
using StreamJsonRpc;

namespace DustyEngineEditor;

internal static class Runner
{
    private const string ProjectPath = "/home/maksym/DustyEngine/TestProject";
    private const string RunnerPath = "/home/maksym/DustyEngine/Runner/bin/Debug/net9.0/Runner";
    private static void Main(string[] args) => new Editor(ProjectPath, RunnerPath);
}

internal class Editor
{
    public const float EditorVersion = 0.01f;

    private readonly string _projectPath;
    private readonly string _runnerPath;

    private Process? _engineProcess;
    private volatile bool _engineRunning;

    public Editor(string projectPath, string runnerPath)
    {
        _projectPath = projectPath;
        _runnerPath = runnerPath;

        StartEngineProcess();
        ConnectToEngineByStreamJsonRpc();
    }

    private void StartEngineProcess()
    {
        if (_engineRunning) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _runnerPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            psi.ArgumentList.Add(_projectPath);
            psi.ArgumentList.Add("Editor");

            _engineProcess = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            _engineProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    Console.WriteLine($"[DustyEngine] {e.Data}");
            };

            _engineProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    Console.WriteLine($"[DustyEngine ERROR] {e.Data}");
            };

            _engineProcess.Exited += (_, __) =>
            {
                Console.WriteLine("DustyEngine exited.");
                _engineRunning = false;
            };

            _engineProcess.Start();
            _engineProcess.BeginOutputReadLine();
            _engineProcess.BeginErrorReadLine();

            _engineRunning = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to start Runner: " + ex.Message);
            _engineRunning = false;
        }
    }


    private static void ConnectToEngineByStreamJsonRpc()
    {
        var stream = new NamedPipeClientStream(".", "StreamJsonRpcSamplePipe", PipeDirection.InOut,
            PipeOptions.Asynchronous);
        stream.Connect();

        var jsonRpc = JsonRpc.Attach(stream);
        var renderer = jsonRpc.Attach<IRemoteRenderer>();

        using (var clientWindow = new ViewportRenderer(renderer))
            clientWindow.Run();

        stream.Dispose();
    }
}