using System.Diagnostics;
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



public class DustyEditorWindow : GameWindow
{
    private ImGuiManager _imguiManager;
    private Process _engineProcess;
    private bool _engineRunning = false;
    private DateTime _lastFpsUpdate = DateTime.Now;
    private int _frameCount = 0;
    private int _currentFps = 0;
    private readonly string _projectPath = "/home/maksym/github/DustyEngine/TestProject";

    public DustyEditorWindow() : base(
        new GameWindowSettings()
        {
            UpdateFrequency = 60.0,
        },
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

        _imguiManager = new ImGuiManager();
        _imguiManager.GetFPS = () => _currentFps;
        _imguiManager.GetSceneObjectCount = () => _engineRunning ? 5 : 0; 
        _imguiManager.GetSceneTexture = () => 0; 
        _imguiManager.GetSceneSize = () => (ClientSize.X, ClientSize.Y);
        
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
    }

    private void RenderMainMenuBar()
    {
        if (ImGuiNET.ImGui.BeginMainMenuBar())
        {
            if (ImGuiNET.ImGui.BeginMenu("File"))
            {
                if (ImGuiNET.ImGui.MenuItem("New Project"))
                {
                    Console.WriteLine("New Project clicked");
                }
                if (ImGuiNET.ImGui.MenuItem("Open Project"))
                {
                    Console.WriteLine("Open Project clicked");
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
            
            ImGuiNET.ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), "[EDITOR] DustyEditor started successfully");
            
            if (_engineRunning)
            {
                ImGuiNET.ImGui.TextColored(new System.Numerics.Vector4(0, 0.8f, 1, 1), "[ENGINE] Engine process is running...");
            }
            else
            {
                ImGuiNET.ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "[ENGINE] Engine is not running");
            }
            
            ImGuiNET.ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), "[INFO] Use Engine menu to start/stop engine process");
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
                CreateNoWindow = true
            };
            psi.ArgumentList.Add(_projectPath);

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
                Console.WriteLine($"[ENGINE] Process exited with code {_engineProcess?.ExitCode}");
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
        }
    }

    private void StopEngineProcess()
    {
        if (!_engineRunning || _engineProcess == null) return;
        
        try
        {
            _engineProcess.Kill();
            _engineProcess.WaitForExit(5000); // Ждем 5 секунд
            _engineRunning = false;
            Console.WriteLine("[EDITOR] Engine process stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EDITOR] Error stopping engine: {ex.Message}");
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
    }

    protected override void OnUnload()
    {
        StopEngineProcess();
        _imguiManager?.Shutdown();
        Console.WriteLine("DustyEditor shutting down...");
        base.OnUnload();
    }
}