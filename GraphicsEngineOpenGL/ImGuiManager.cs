using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ImGui_OpenTK.Backends;
using DustyEngine;

namespace GraphicsEngineOpenGL;

public class ImGuiManager
{
    private bool _initialized = false;
    private GameWindow _window;
    
    // Callbacks для получения данных от Window
    public Func<int> GetSceneObjectCount { get; set; } = () => 0;
    public Func<int> GetFPS { get; set; } = () => 0;
    public Func<int> GetSceneTexture { get; set; } = () => 0;
    public Func<(int width, int height)> GetSceneSize { get; set; } = () => (800, 600);
    public Action<int, int> OnSceneResize { get; set; }

    public bool IsInitialized => _initialized;

    public void Initialize(GameWindow window)
    {
        _window = window;
        
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();

        ImguiImplOpenTK4.Init(window);
        ImguiImplOpenGL3.Init();
        
        _initialized = true;
        Debug.Log("ImGui initialized", Debug.LogLevel.Info, true);
    }

    public void NewFrame()
    {
        if (!_initialized) return;

        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();
    }

    public void Render()
    {
        if (!_initialized) return;

        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
    }

    public void RenderUI()
    {
        if (!_initialized) return;

        ImGui.DockSpaceOverViewport();

        // Settings Panel
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Settings Panel"))
        {
            ImGui.Text($"Scene Objects: {GetSceneObjectCount?.Invoke() ?? 0}");
            ImGui.Text($"FPS: {GetFPS?.Invoke() ?? 0}");
        }
        ImGui.End();

        // Main Scene Viewport Panel
        RenderSceneViewport();
    }

    private void RenderSceneViewport()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 170), ImGuiCond.FirstUseEver);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        ImGui.Begin("Main Scene Viewport", ImGuiWindowFlags.NoCollapse);
        
        var availableSize = ImGui.GetContentRegionAvail();
        if (availableSize.X > 32 && availableSize.Y > 32)
        {
            float targetAspectRatio = 16.0f / 9.0f;
            float availableAspectRatio = availableSize.X / availableSize.Y;
            System.Numerics.Vector2 imageSize;
            
            if (availableAspectRatio > targetAspectRatio)
            {
                imageSize.Y = availableSize.Y;
                imageSize.X = imageSize.Y * targetAspectRatio;
            }
            else
            {
                imageSize.X = availableSize.X;
                imageSize.Y = imageSize.X / targetAspectRatio;
            }
            
            int targetWidth = (int)imageSize.X;
            int targetHeight = (int)imageSize.Y;
            
            // Уведомляем Window о необходимости изменить размер
            var currentSize = GetSceneSize?.Invoke() ?? (800, 600);
            if (Math.Abs(targetWidth - currentSize.width) > currentSize.width * 0.1f ||
                Math.Abs(targetHeight - currentSize.height) > currentSize.height * 0.1f)
            {
                OnSceneResize?.Invoke(targetWidth, targetHeight);
            }
            
            // Центрируем изображение
            var cursor = ImGui.GetCursorPos();
            cursor.X += (availableSize.X - imageSize.X) * 0.5f;
            cursor.Y += (availableSize.Y - imageSize.Y) * 0.5f;
            ImGui.SetCursorPos(cursor);
            
            // Отображаем изображение
            int textureId = GetSceneTexture?.Invoke() ?? 0;
            ImGui.Image(new IntPtr(textureId), imageSize, 
                new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
        }
        
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    public void Shutdown()
    {
        if (!_initialized) return;
        
        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        
        _initialized = false;
        Debug.Log("ImGui shutdown", Debug.LogLevel.Info, true);
    }
}