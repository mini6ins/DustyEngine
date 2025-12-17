using System.Diagnostics;
using System.IO.Pipes;
using DustyEngineEditor.Panels;
using DustyEngineEditor.Panels.ConsolePanel;
using DustyEngineEditor.Panels.ViewPortPanel.RemoteRenderer;
using StreamJsonRpc;

namespace DustyEngineEditor;

internal class Editor
{
    public const float EditorVersion = 0.01f;

    public static string? ProjectPath { get; private set; }
    private readonly string _runnerPath;

    private Process? _engineProcess;
    private volatile bool _engineRunning;

    public static IRemoteRenderer? RemoteRenderer { get; private set; }

    public static int Main(string[] args)
    {
        var projectPath = args.Length > 0 ? args[0] : ".";
        var runnerPath = args.Length > 1 ? args[1] : "";

        if (string.IsNullOrWhiteSpace(runnerPath))
        {
            ConsolePanel.Log("DustyEngine path is empty. Pass: <projectPath> <runnerPath>");
            return 1;
        }

        var editor = new Editor(projectPath, runnerPath);
        editor.Run();
        return 0;
    }

    private Editor(string projectPath, string runnerPath)
    {
        ProjectPath = projectPath;
        _runnerPath = runnerPath;
    }

    private void Run()
    {
        StartEngineProcess();

        try
        {
            ConnectToEngineByStreamJsonRpc();
        }
        finally
        {
            StopEngineProcess();
        }
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

            psi.ArgumentList.Add(ProjectPath ?? ".");
            psi.ArgumentList.Add("Editor");

            _engineProcess = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            _engineProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) ConsolePanel.Out($"{e.Data}");
            };

            _engineProcess.Start();
            _engineProcess.BeginOutputReadLine();
            _engineProcess.BeginErrorReadLine();

            _engineRunning = true;
        }
        catch (Exception ex)
        {
            ConsolePanel.Log("Failed to start DustyEngine: " + ex.Message);
            _engineRunning = false;
        }
    }

    private void StopEngineProcess()
    {
        if (!_engineRunning || _engineProcess == null)
            return;

        try
        {
            if (_engineProcess.HasExited) return;

            _engineProcess.CloseMainWindow();

            if (_engineProcess.WaitForExit(2000)) return;

            _engineProcess.Kill(entireProcessTree: true);
            _engineProcess.WaitForExit();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _engineProcess.Dispose();
            _engineProcess = null;
            _engineRunning = false;
        }
    }

    private static void ConnectToEngineByStreamJsonRpc()
    {
        using var stream = new NamedPipeClientStream(".", "StreamJsonRpcSamplePipe", PipeDirection.InOut,
            PipeOptions.Asynchronous);

        stream.Connect();

        var jsonRpc = JsonRpc.Attach(stream);
        RemoteRenderer = jsonRpc.Attach<IRemoteRenderer>();

        using var clientWindow = new ViewportRenderer();
        clientWindow.Run();
    }
}
