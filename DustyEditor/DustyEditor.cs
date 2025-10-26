using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace DustyEditor;

internal class DustyEditor
{
    private readonly string _projectPath = "/home/maksym/github/DustyEngine/TestProject";
    private readonly string _runnerPath = "/home/maksym/github/DustyEngine/Runner/bin/Debug/net9.0/Runner";
    private readonly string _mmfPath;

    private Process? _engineProcess;
    private volatile bool _engineRunning;
    private CancellationTokenSource? _cts;

    public DustyEditor()
    {
        _mmfPath = Directory.Exists("/dev/shm")
            ? "/dev/shm/vid_stream.mmf"
            : Path.Combine(Path.GetTempPath(), "vid_stream.mmf");
    }

    public static void Main(string[] args)
        => new DustyEditor().Run();

    private void Run()
    {
        var gws = GameWindowSettings.Default;
        var nws = new NativeWindowSettings { ClientSize = new Vector2i(800, 600), Title = "Dusty Editor" };

        using var frameReceiver = new FrameReceiver(_mmfPath);
        using var window = new RenderWindow(gws, nws, frameReceiver);

        frameReceiver.OnConnectionLost += reason => { _ = SafeReconnectLoop(frameReceiver); };

        window.Closing += _ =>
        {
            _cts?.Cancel();
            StopEngineProcess();
        };

        StartEngineProcess();
        _cts = new CancellationTokenSource();
        _ = ConnectLoop(frameReceiver, _cts.Token);

        window.Run();
    }

    private async Task ConnectLoop(FrameReceiver receiver, CancellationToken token)
    {
        var delay = TimeSpan.FromMilliseconds(300);
        var max = TimeSpan.FromSeconds(3);

        while (!token.IsCancellationRequested)
        {
            if (!_engineRunning)
                StartEngineProcess();

            try
            {
                if (await receiver.ConnectAsync())
                {
                    return;
                }
            }
            catch
            {
                // ignored
            }

            await Task.Delay(delay, token);
            delay = TimeSpan.FromMilliseconds(Math.Math.Min(delay.TotalMilliseconds * 1.8, max.TotalMilliseconds));
        }
    }

    private async Task SafeReconnectLoop(FrameReceiver receiver)
    {
        var delay = TimeSpan.FromMilliseconds(300);
        var max = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= 20; attempt++)
        {
            if (!_engineRunning)
                StartEngineProcess();

            try
            {
                if (await receiver.ConnectAsync())
                {
                    return;
                }
            }
            catch
            {
                // ignored
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromMilliseconds(Math.Math.Min(delay.TotalMilliseconds * 1.7, max.TotalMilliseconds));
        }

        RestartEngineProcess();
        _ = ConnectLoop(receiver, _cts?.Token ?? CancellationToken.None);
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
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add(_projectPath);
            psi.ArgumentList.Add("Context");

            psi.Environment["DUSTY_MMF_PATH"] = _mmfPath;

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

    private void StopEngineProcess()
    {
        try
        {
            if (_engineProcess is not { HasExited: false }) return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _engineProcess.CloseMainWindow();
            }
            else
            {
                _engineProcess.Kill(entireProcessTree: true);
            }

            _engineProcess.WaitForExit(2000);
        }
        catch
        {
            // ignored
        }
        finally
        {
            _engineRunning = false;
            _engineProcess?.Dispose();
            _engineProcess = null;
        }
    }

    private void RestartEngineProcess()
    {
        StopEngineProcess();
        StartEngineProcess();
    }
}