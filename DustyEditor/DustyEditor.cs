using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using GraphicsEngineOpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace DustyEditor;

internal class DustyEditor
{
    private readonly string _projectPath = "/home/maksym/github/DustyEngine/TestProject";
    private readonly string _runnerPath  = "/home/maksym/github/DustyEngine/Runner/bin/Debug/net9.0/Runner";
    private readonly string _serverHost  = "127.0.0.1";
    private readonly int    _serverPort  = 8080;

    private Process? _engineProcess;
    private volatile bool _engineRunning;
    private CancellationTokenSource? _cts;

    public static void Main(string[] args)
        => new DustyEditor().Run();

    private void Run()
    {
        var gws = GameWindowSettings.Default;
        var nws = new NativeWindowSettings { Size = new Vector2i(800, 600), Title = "Dusty Editor" };

        using var frameReceiver = new FrameReceiver(_serverHost, _serverPort);
        using var window = new RenderWindow(gws, nws, frameReceiver);

        frameReceiver.OnConnected += () => Console.WriteLine("=== ПОДКЛЮЧЕН К СЕРВЕРУ ===");
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
        StartEngineProcess();             // Runner поднимает сервер
        _cts = new CancellationTokenSource();
        _ = ConnectLoop(frameReceiver, _cts.Token);

        Console.WriteLine("=== КЛИЕНТ РЕНДЕРИНГА ===");
        Console.WriteLine("Получаю фреймбуфер от сервера...");
        Console.WriteLine("ESC — выход");
        Console.WriteLine("=========================");

        window.Run();
    }

    // ——— АВТО-ПОДКЛЮЧЕНИЕ / ПЕРЕПОДКЛЮЧЕНИЕ ———

    private async Task ConnectLoop(FrameReceiver receiver, CancellationToken token)
    {
        var delay = TimeSpan.FromMilliseconds(300);   // начальная задержка
        var max   = TimeSpan.FromSeconds(3);

        while (!token.IsCancellationRequested)
        {
            if (!_engineRunning)
                StartEngineProcess();

            try
            {
                await receiver.ConnectAsync();       // метод твоего клиента
                // Успех — ждём, пока соединение не отвалится (событие поднимет SafeReconnectLoop)
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Не удалось подключиться: {ex.Message}");
            }

            await Task.Delay(delay, token);
            // экспоненциальный бэкофф до max
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.8, max.TotalMilliseconds));
        }
    }

    private async Task SafeReconnectLoop(FrameReceiver receiver)
    {
        // отдельный короткий цикл переподключения (без отмены окон)
        var delay = TimeSpan.FromMilliseconds(300);
        var max   = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= 20; attempt++)
        {
            if (!_engineRunning)
                StartEngineProcess();

            try
            {
                await receiver.ConnectAsync();
                Console.WriteLine("[CLIENT] Переподключение успешно");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Переподключение попытка {attempt}: {ex.Message}");
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.7, max.TotalMilliseconds));
        }

        Console.WriteLine("[CLIENT] Переподключиться не удалось. Пробую перезапустить движок и снова подключиться.");
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

            // Аргументы: путь проекта и режим рендера (если поддерживается твоим Runner’ом)
            psi.ArgumentList.Add(_projectPath);
            psi.ArgumentList.Add("Context"); // или "Standalone", если хочешь — можно сделать настраиваемым

            // (Необязательно) Пробрасываем порт в окружение, если сервер умеет читать:
            psi.Environment["DUSTY_SERVER_PORT"] = _serverPort.ToString();

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
                // посылаем корректное завершение
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _engineProcess.CloseMainWindow();
                }
                else
                {
                    // на Linux часто нет окна — шлём SIGINT-эквивалент
                    _engineProcess.Kill(entireProcessTree: true);
                }

                _engineProcess.WaitForExit(2000);
            }
        }
        catch { /* игнорируем при выходе */ }
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
