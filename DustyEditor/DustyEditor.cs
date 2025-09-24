using System.Diagnostics;
using System.Text.Json;
using GraphicsEngineOpenGL;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DustyEditor;

internal static class DustyEditor
{
    static void Main(string[] args)
    {
       using var editor = new DustyEditorWindow();
       editor.Run();
    }
}


public class EngineData
{
    public int TextureId { get; set; }
    public (int width, int height) FramebufferSize { get; set; }
    public int ObjectCount { get; set; }
    public int FPS { get; set; }
    public long Timestamp { get; set; }
    public string PixelDataPath { get; set; }
}

public class DustyEditorWindow : GameWindow
{
    private ImGuiManager _imguiManager;
    private Process _engineProcess;
    private bool _engineRunning = false;
    private DateTime _lastFpsUpdate = DateTime.Now;
    private int _frameCount = 0;
    private int _currentFps = 0;
    private readonly string _projectPath = "/home/maksym/github/DustyEngine/TestProject";
    
    // Данные от движка
    private EngineData _currentEngineData = new EngineData();
    private readonly object _engineDataLock = new object();
    private List<string> _consoleMessages = new List<string>();
    private readonly object _consoleLock = new object();
    
    // OpenGL текстура для отображения кадра от движка
    private int _engineTexture = 0;
    private int _textureWidth = 800;
    private int _textureHeight = 600;

    public DustyEditorWindow() : base(
        new GameWindowSettings() { UpdateFrequency = 60.0 },
        new NativeWindowSettings()
        {
            Title = "DustyEngine Editor",
            Size = new OpenTK.Mathematics.Vector2i(1400, 900),
            WindowBorder = WindowBorder.Resizable,
            StartVisible = true,
            StartFocused = true,
            API = ContextAPI.OpenGL,
            Profile = ContextProfile.Core,
            APIVersion = new Version(4, 5)
        })
    {
        Console.WriteLine("DustyEditor window created");
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        // Создаем пустую текстуру для движка
        CreateEngineTexture(_textureWidth, _textureHeight);

        _imguiManager = new ImGuiManager();
        
        _imguiManager.GetFPS = () => 
        {
            lock (_engineDataLock)
            {
                return _engineRunning ? _currentEngineData.FPS : _currentFps;
            }
        };
        
        _imguiManager.GetSceneObjectCount = () => 
        {
            lock (_engineDataLock)
            {
                return _engineRunning ? _currentEngineData.ObjectCount : 0;
            }
        };
        
        _imguiManager.GetSceneTexture = () => _engineTexture;
        
        _imguiManager.GetSceneSize = () => (_textureWidth, _textureHeight);
        
        _imguiManager.OnSceneResize = (w, h) => 
        {
            SendCommandToEngine($"RESIZE:{w},{h}");
        };
        
        bool initialized = _imguiManager.Initialize(this);
        
        if (initialized)
        {
            Console.WriteLine("ImGui successfully initialized with OpenGL context");
        }
        else
        {
            Console.WriteLine("Failed to initialize ImGui");
        }
    }

    private void CreateEngineTexture(int width, int height)
    {
        if (_engineTexture != 0)
        {
            GL.DeleteTexture(_engineTexture);
        }

        _engineTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _engineTexture);
        
        // Создаем пустую текстуру
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8, 
            width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        GL.BindTexture(TextureTarget.Texture2d, 0);
        
        _textureWidth = width;
        _textureHeight = height;
    }

    private void UpdateEngineTexture(string pixelDataPath, int width, int height)
    {
        if (!File.Exists(pixelDataPath)) return;

        try
        {
            // Пересоздаем текстуру если размер изменился
            if (width != _textureWidth || height != _textureHeight)
            {
                CreateEngineTexture(width, height);
            }

            // Загружаем пиксельные данные
            var pixelData = File.ReadAllBytes(pixelDataPath);
            
            GL.BindTexture(TextureTarget.Texture2d, _engineTexture);
            GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, width, height, 
                PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
            GL.BindTexture(TextureTarget.Texture2d, 0);

            // Удаляем файл после использования
            File.Delete(pixelDataPath);
        }
        catch (Exception ex)
        {
            AddConsoleMessage($"[EDITOR] Error updating texture: {ex.Message}");
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        _frameCount++;
        if ((DateTime.Now - _lastFpsUpdate).TotalSeconds >= 1.0)
        {
            _currentFps = _frameCount;
            _frameCount = 0;
            _lastFpsUpdate = DateTime.Now;
        }
        
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        if (_imguiManager != null && _imguiManager.IsInitialized)
        {
            _imguiManager.NewFrame();
            RenderEditorUI();
            _imguiManager.Render();
        }
        
        SwapBuffers();
    }

    private void RenderEditorUI()
    {
        _imguiManager.RenderUI();
        RenderMainMenuBar();
        RenderProjectPanel();
        RenderConsolePanel();
        RenderEngineStatus();
    }

    private void RenderMainMenuBar()
    {
        if (ImGuiNET.ImGui.BeginMainMenuBar())
        {
            if (ImGuiNET.ImGui.BeginMenu("File"))
            {
                if (ImGuiNET.ImGui.MenuItem("New Project"))
                {
                    AddConsoleMessage("[EDITOR] New Project clicked");
                }
                if (ImGuiNET.ImGui.MenuItem("Open Project"))
                {
                    AddConsoleMessage("[EDITOR] Open Project clicked");
                }
                ImGuiNET.ImGui.Separator();
                if (ImGuiNET.ImGui.MenuItem("Exit"))
                {
                    Close();
                }
                ImGuiNET.ImGui.EndMenu();
            }
            
            if (ImGuiNET.ImGui.BeginMenu("Engine"))
            {
                if (ImGuiNET.ImGui.MenuItem("Start Engine", null, false, !_engineRunning))
                {
                    StartEngineProcess();
                }
                if (ImGuiNET.ImGui.MenuItem("Stop Engine", null, false, _engineRunning))
                {
                    StopEngineProcess();
                }
                ImGuiNET.ImGui.Separator();
                if (ImGuiNET.ImGui.MenuItem("Restart Engine", null, false, _engineRunning))
                {
                    StopEngineProcess();
                    Task.Delay(1000).ContinueWith(_ => StartEngineProcess());
                }
                ImGuiNET.ImGui.EndMenu();
            }
            
            ImGuiNET.ImGui.EndMainMenuBar();
        }
    }

    private void RenderProjectPanel()
    {
        ImGuiNET.ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 400), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new System.Numerics.Vector2(1090, 30), ImGuiNET.ImGuiCond.FirstUseEver);
        
        if (ImGuiNET.ImGui.Begin("Project"))
        {
            ImGuiNET.ImGui.Text("Project Explorer");
            ImGuiNET.ImGui.Separator();
            
            ImGuiNET.ImGui.Text($"Current Project:");
            ImGuiNET.ImGui.TextWrapped(_projectPath);
            
            ImGuiNET.ImGui.Spacing();
            
            if (ImGuiNET.ImGui.TreeNode("Assets"))
            {
                ImGuiNET.ImGui.Selectable("Textures/");
                ImGuiNET.ImGui.Selectable("Models/");
                ImGuiNET.ImGui.Selectable("Scripts/");
                ImGuiNET.ImGui.TreePop();
            }
            
            if (ImGuiNET.ImGui.TreeNode("Scenes"))
            {
                ImGuiNET.ImGui.Selectable("Main.scene");
                ImGuiNET.ImGui.Selectable("Menu.scene");
                ImGuiNET.ImGui.TreePop();
            }
        }
        ImGuiNET.ImGui.End();
    }

    private void RenderConsolePanel()
    {
        ImGuiNET.ImGui.SetNextWindowSize(new System.Numerics.Vector2(1080, 200), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 690), ImGuiNET.ImGuiCond.FirstUseEver);
        
        if (ImGuiNET.ImGui.Begin("Console"))
        {
            ImGuiNET.ImGui.Text("Engine Console Output");
            ImGuiNET.ImGui.Separator();
            
            lock (_consoleLock)
            {
                foreach (var message in _consoleMessages.TakeLast(20))
                {
                    var color = GetMessageColor(message);
                    ImGuiNET.ImGui.TextColored(color, message);
                }
            }
            
            if (ImGuiNET.ImGui.GetScrollY() >= ImGuiNET.ImGui.GetScrollMaxY())
                ImGuiNET.ImGui.SetScrollHereY(1.0f);
        }
        ImGuiNET.ImGui.End();
    }

    private void RenderEngineStatus()
    {
        ImGuiNET.ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 150), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new System.Numerics.Vector2(1090, 450), ImGuiNET.ImGuiCond.FirstUseEver);
        
        if (ImGuiNET.ImGui.Begin("Engine Status"))
        {
            ImGuiNET.ImGui.Text("Engine Status");
            ImGuiNET.ImGui.Separator();
            
            var statusColor = _engineRunning ? new System.Numerics.Vector4(0, 1, 0, 1) : new System.Numerics.Vector4(1, 0, 0, 1);
            var statusText = _engineRunning ? "Running" : "Stopped";
            ImGuiNET.ImGui.TextColored(statusColor, $"Status: {statusText}");
            
            if (_engineRunning)
            {
                lock (_engineDataLock)
                {
                    ImGuiNET.ImGui.Text($"Engine FPS: {_currentEngineData.FPS}");
                    ImGuiNET.ImGui.Text($"Objects: {_currentEngineData.ObjectCount}");
                    ImGuiNET.ImGui.Text($"FB Size: {_currentEngineData.FramebufferSize.width}x{_currentEngineData.FramebufferSize.height}");
                    ImGuiNET.ImGui.Text($"Texture ID: {_engineTexture}");
                }
            }
            
            ImGuiNET.ImGui.Text($"Editor FPS: {_currentFps}");
        }
        ImGuiNET.ImGui.End();
    }

    private void StartEngineProcess()
    {
        if (_engineRunning) return;
        
        try
        {
            var runnerPath = "/home/maksym/github/DustyEngine/Runner/bin/Debug/net9.0/Runner";
            
            var psi = new ProcessStartInfo
            {
                FileName = runnerPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add(_projectPath);
            psi.ArgumentList.Add("--headless");

            _engineProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            _engineProcess.OutputDataReceived += OnEngineOutput;
            _engineProcess.ErrorDataReceived += OnEngineError;
            _engineProcess.Exited += OnEngineExited;

            _engineProcess.Start();
            _engineProcess.BeginOutputReadLine();
            _engineProcess.BeginErrorReadLine();
            
            _engineRunning = true;
            AddConsoleMessage("[EDITOR] Engine process started in headless mode");
        }
        catch (Exception ex)
        {
            AddConsoleMessage($"[EDITOR] Failed to start engine: {ex.Message}");
        }
    }

    private void OnEngineOutput(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;
        
        if (e.Data.StartsWith("ENGINE_DATA:"))
        {
            try
            {
                var jsonData = e.Data.Substring("ENGINE_DATA:".Length);
                var engineData = JsonSerializer.Deserialize<EngineData>(jsonData);
                
                lock (_engineDataLock)
                {
                    _currentEngineData = engineData;
                }

                // Обновляем текстуру если есть новые пиксельные данные
                if (!string.IsNullOrEmpty(engineData.PixelDataPath))
                {
                    var size = engineData.FramebufferSize;
                    UpdateEngineTexture(engineData.PixelDataPath, size.width, size.height);
                }
            }
            catch (Exception ex)
            {
                AddConsoleMessage($"[EDITOR] Failed to parse engine data: {ex.Message}");
            }
        }
        else
        {
            AddConsoleMessage("[ENGINE] " + e.Data);
        }
    }

    private void OnEngineError(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            AddConsoleMessage("[ENGINE-ERR] " + e.Data);
        }
    }

    private void OnEngineExited(object sender, EventArgs e)
    {
        AddConsoleMessage($"[ENGINE] Process exited with code {_engineProcess?.ExitCode}");
        _engineRunning = false;
    }

    private void StopEngineProcess()
    {
        if (!_engineRunning || _engineProcess == null) return;
        
        try
        {
            SendCommandToEngine("QUIT");
            
            if (!_engineProcess.WaitForExit(3000))
            {
                _engineProcess.Kill();
            }
            
            _engineRunning = false;
            AddConsoleMessage("[EDITOR] Engine process stopped");
        }
        catch (Exception ex)
        {
            AddConsoleMessage($"[EDITOR] Error stopping engine: {ex.Message}");
        }
    }

    private void SendCommandToEngine(string command)
    {
        if (_engineProcess != null && !_engineProcess.HasExited)
        {
            try
            {
                _engineProcess.StandardInput.WriteLine(command);
                _engineProcess.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                AddConsoleMessage($"[EDITOR] Failed to send command: {ex.Message}");
            }
        }
    }

    private void AddConsoleMessage(string message)
    {
        lock (_consoleLock)
        {
            _consoleMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            if (_consoleMessages.Count > 100)
            {
                _consoleMessages.RemoveAt(0);
            }
        }
    }

    private System.Numerics.Vector4 GetMessageColor(string message)
    {
        if (message.Contains("[ENGINE-ERR]") || message.Contains("ERROR") || message.Contains("FATAL"))
            return new System.Numerics.Vector4(1, 0, 0, 1);
        
        if (message.Contains("[ENGINE]"))
            return new System.Numerics.Vector4(0, 0.8f, 1, 1);
            
        if (message.Contains("WARNING"))
            return new System.Numerics.Vector4(1, 1, 0, 1);
            
        if (message.Contains("[EDITOR]"))
            return new System.Numerics.Vector4(0, 1, 0, 1);
            
        return new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
    }

    protected override void OnUnload()
    {
        StopEngineProcess();
        
        if (_engineTexture != 0)
        {
            GL.DeleteTexture(_engineTexture);
        }
        
        _imguiManager?.Shutdown();
        Console.WriteLine("DustyEditor shutting down...");
        base.OnUnload();
    }
}