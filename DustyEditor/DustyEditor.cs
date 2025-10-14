using System.Diagnostics;
using System.Runtime.InteropServices;
using GraphicsEngineOpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace DustyEditor;

internal class DustyEditor
{
    private readonly string _projectPath = "/home/maksym/github/DustyEngine/TestProject";
    private readonly string _runnerPath  = "/home/maksym/github/DustyEngine/Runner/bin/Debug/net9.0/Runner";
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
        var nws = new NativeWindowSettings { Size = new Vector2i(800, 600), Title = "Dusty Editor" };

        using var frameReceiver = new FrameReceiver(_mmfPath);
        using var window = new RenderWindow(gws, nws, frameReceiver);

        frameReceiver.OnConnected += () => Console.WriteLine("=== ПОДКЛЮЧЕН К MMF ===");
        frameReceiver.OnConnectionLost += reason =>
        {
            Console.WriteLine($"[CLIENT] Соединение потеряно: {reason}");
            _ = SafeReconnectLoop(frameReceiver);
        };

        window.Closing += _ =>
        {
            _cts?.Cancel();
            StopEngineProcess();
        };

        // Стартуем движок и сразу запускаем цикл автоподключения
        StartEngineProcess();
        _cts = new CancellationTokenSource();
        _ = ConnectLoop(frameReceiver, _cts.Token);

        Console.WriteLine("=== КЛИЕНТ РЕНДЕРИНГА (MMF) ===");
        Console.WriteLine("Получаю фреймбуфер из Shared Memory...");
        Console.WriteLine("ESC — выход");
        Console.WriteLine("===============================");

        window.Run();
    }

    // ——— АВТО-ПОДКЛЮЧЕНИЕ / ПЕРЕПОДКЛЮЧЕНИЕ ———

    private async Task ConnectLoop(FrameReceiver receiver, CancellationToken token)
    {
        var delay = TimeSpan.FromMilliseconds(300);
        var max   = TimeSpan.FromSeconds(3);

        while (!token.IsCancellationRequested)
        {
            if (!_engineRunning)
                StartEngineProcess();

            try
            {
                if (await receiver.ConnectAsync())
                {
                    // Успех — ждём, пока соединение не отвалится
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Не удалось подключиться к MMF: {ex.Message}");
            }

            await Task.Delay(delay, token);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.8, max.TotalMilliseconds));
        }
    }

    private async Task SafeReconnectLoop(FrameReceiver receiver)
    {
        var delay = TimeSpan.FromMilliseconds(300);
        var max   = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= 20; attempt++)
        {
            if (!_engineRunning)
                StartEngineProcess();

            try
            {
                if (await receiver.ConnectAsync())
                {
                    Console.WriteLine("[CLIENT] Переподключение к MMF успешно");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Переподключение попытка {attempt}: {ex.Message}");
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.7, max.TotalMilliseconds));
        }

        Console.WriteLine("[CLIENT] Переподключиться не удалось. Пробую перезапустить движок.");
        RestartEngineProcess();
        _ = ConnectLoop(receiver, _cts?.Token ?? CancellationToken.None);
    }

    // ——— УПРАВЛЕНИЕ ПРОЦЕССОМ ДВИЖКА ———

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
                RedirectStandardError  = true,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add(_projectPath);
            psi.ArgumentList.Add("Context");

            // Указываем путь к MMF в окружении (если Runner это поддерживает)
            psi.Environment["DUSTY_MMF_PATH"] = _mmfPath;

            _engineProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            _engineProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine("[ENGINE] " + e.Data);
            };

            _engineProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.Error.WriteLine("[ENGINE-ERR] " + e.Data);
            };

            _engineProcess.Exited += (_, __) =>
            {
                Console.WriteLine($"[ENGINE] Exit code: {_engineProcess?.ExitCode}");
                _engineRunning = false;
            };

            _engineProcess.Start();
            _engineProcess.BeginOutputReadLine();
            _engineProcess.BeginErrorReadLine();

            _engineRunning = true;
            Console.WriteLine("[EDITOR] Engine process started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EDITOR] Failed to start engine: {ex.Message}");
            _engineRunning = false;
        }
    }

    private void StopEngineProcess()
    {
        try
        {
            if (_engineProcess is { HasExited: false })
            {
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
        }
        catch { }
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