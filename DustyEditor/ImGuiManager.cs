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
    private bool _hasValidContext = false;
    
    public Func<int> GetSceneObjectCount { get; set; } = () => 0;
    public Func<int> GetFPS { get; set; } = () => 0;
    public Func<int> GetSceneTexture { get; set; } = () => 0;
    public Func<(int width, int height)> GetSceneSize { get; set; } = () => (800, 600);
    public Action<int, int> OnSceneResize { get; set; }

    public bool IsInitialized => _initialized;
    public bool HasValidContext => _hasValidContext;

    public bool Initialize(GameWindow window = null)
    {
        try
        {
            _window = window;
            _hasValidContext = window != null;
            
            ImGui.CreateContext();
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            ImGui.StyleColorsDark();

            if (_hasValidContext)
            {
                ImguiImplOpenTK4.Init(window);
                ImguiImplOpenGL3.Init();
                Debug.Log("ImGui initialized with OpenGL context", Debug.LogLevel.Info, true);
            }
            else
            {
                Debug.Log("ImGui initialized without OpenGL context (headless mode)", Debug.LogLevel.Warning, true);
            }
            
            _initialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Failed to initialize ImGui: {ex.Message}", Debug.LogLevel.Error, true);
            _initialized = false;
            _hasValidContext = false;
            return false;
        }
    }

    public void NewFrame()
    {
        if (!_initialized) return;

        try
        {
            if (_hasValidContext)
            {
                ImguiImplOpenGL3.NewFrame();
                ImguiImplOpenTK4.NewFrame();
            }
            ImGui.NewFrame();
        }
        catch (Exception ex)
        {
            Debug.Log($"Error in ImGui NewFrame: {ex.Message}", Debug.LogLevel.Error, true);
            _hasValidContext = false;
        }
    }

    public void Render()
    {
        if (!_initialized) return;

        try
        {
            ImGui.Render();
            
            if (_hasValidContext)
            {
                ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error in ImGui Render: {ex.Message}", Debug.LogLevel.Error, true);
            _hasValidContext = false;
        }
    }

    public void RenderUI()
    {
        if (!_initialized) return;

        try
        {
            if (_hasValidContext)
            {
                ImGui.DockSpaceOverViewport();
            }
            
            RenderSettingsPanel();
            RenderSceneViewport();
        }
        catch (Exception ex)
        {
            Debug.Log($"Error in ImGui RenderUI: {ex.Message}", Debug.LogLevel.Error, true);
        }
    }

    private void RenderSettingsPanel()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 200), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("Settings Panel"))
        {
            // Основная информация
            ImGui.Text($"Scene Objects: {GetSceneObjectCount?.Invoke() ?? 0}");
            ImGui.Text($"FPS: {GetFPS?.Invoke() ?? 0}");
            
            ImGui.Separator();
            
            // Статус системы
            ImGui.Text("System Status:");
            ImGui.TextColored(_initialized ? new System.Numerics.Vector4(0, 1, 0, 1) : new System.Numerics.Vector4(1, 0, 0, 1), 
                $"ImGui: {(_initialized ? "Initialized" : "Not Initialized")}");
            
            ImGui.TextColored(_hasValidContext ? new System.Numerics.Vector4(0, 1, 0, 1) : new System.Numerics.Vector4(1, 1, 0, 1), 
                $"OpenGL Context: {(_hasValidContext ? "Available" : "Not Available")}");
            
            if (_window != null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"Window: Connected");
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "Window: Not Connected (Headless Mode)");
            }
        }
        ImGui.End();
    }

    private void RenderSceneViewport()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 220), ImGuiCond.FirstUseEver);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        
        if (ImGui.Begin("Main Scene Viewport", ImGuiWindowFlags.NoCollapse))
        {
            var availableSize = ImGui.GetContentRegionAvail();
            
            if (!_hasValidContext)
            {
                // Показываем сообщение об отсутствии контекста
                var textSize = ImGui.CalcTextSize("No OpenGL Context Available");
                var center = new System.Numerics.Vector2(
                    availableSize.X * 0.5f - textSize.X * 0.5f,
                    availableSize.Y * 0.5f - textSize.Y * 0.5f
                );
                ImGui.SetCursorPos(center);
                ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "No OpenGL Context Available");
                
                center.Y += 30;
                var helpText = "Initialize with a valid GameWindow to display scene";
                var helpSize = ImGui.CalcTextSize(helpText);
                center.X = availableSize.X * 0.5f - helpSize.X * 0.5f;
                ImGui.SetCursorPos(center);
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), helpText);
            }
            else if (availableSize.X > 32 && availableSize.Y > 32)
            {
                int textureId = GetSceneTexture?.Invoke() ?? 0;
                
                if (textureId == 0)
                {
                    // Показываем сообщение об отсутствии текстуры
                    var textSize = ImGui.CalcTextSize("No Scene Texture Available");
                    var center = new System.Numerics.Vector2(
                        availableSize.X * 0.5f - textSize.X * 0.5f,
                        availableSize.Y * 0.5f - textSize.Y * 0.5f
                    );
                    ImGui.SetCursorPos(center);
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0, 1), "No Scene Texture Available");
                    
                    center.Y += 30;
                    var helpText = "Scene rendering may not be initialized";
                    var helpSize = ImGui.CalcTextSize(helpText);
                    center.X = availableSize.X * 0.5f - helpSize.X * 0.5f;
                    ImGui.SetCursorPos(center);
                    ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), helpText);
                }
                else
                {
                    // Отображаем текстуру сцены с правильным соотношением сторон
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
                    
                    var currentSize = GetSceneSize?.Invoke() ?? (800, 600);
                    if (Math.Abs(targetWidth - currentSize.width) > currentSize.width * 0.1f ||
                        Math.Abs(targetHeight - currentSize.height) > currentSize.height * 0.1f)
                    {
                        OnSceneResize?.Invoke(targetWidth, targetHeight);
                    }
                    
                    var cursor = ImGui.GetCursorPos();
                    cursor.X += (availableSize.X - imageSize.X) * 0.5f;
                    cursor.Y += (availableSize.Y - imageSize.Y) * 0.5f;
                    ImGui.SetCursorPos(cursor);
                    
                    ImGui.Image(new IntPtr(textureId), imageSize, 
                        new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
                }
            }
            else
            {
                // Окно слишком маленькое
                var textSize = ImGui.CalcTextSize("Viewport too small");
                var center = new System.Numerics.Vector2(
                    availableSize.X * 0.5f - textSize.X * 0.5f,
                    availableSize.Y * 0.5f - textSize.Y * 0.5f
                );
                ImGui.SetCursorPos(center);
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), "Viewport too small");
            }
        }
        
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    public void Shutdown()
    {
        if (!_initialized) return;
        
        try
        {
            if (_hasValidContext)
            {
                ImguiImplOpenGL3.Shutdown();
                ImguiImplOpenTK4.Shutdown();
                Debug.Log("ImGui OpenGL backends shutdown", Debug.LogLevel.Info, true);
            }
            
            if (ImGui.GetCurrentContext() != IntPtr.Zero)
            {
                ImGui.DestroyContext();
            }
            
            _initialized = false;
            _hasValidContext = false;
            Debug.Log("ImGui shutdown complete", Debug.LogLevel.Info, true);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error during ImGui shutdown: {ex.Message}", Debug.LogLevel.Error, true);
        }
    }

    // Метод для повторной инициализации с окном
    public bool Reinitialize(GameWindow window)
    {
        if (_initialized && !_hasValidContext && window != null)
        {
            try
            {
                _window = window;
                ImguiImplOpenTK4.Init(window);
                ImguiImplOpenGL3.Init();
                _hasValidContext = true;
                Debug.Log("ImGui reinitialized with OpenGL context", Debug.LogLevel.Info, true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to reinitialize ImGui with context: {ex.Message}", Debug.LogLevel.Error, true);
                return false;
            }
        }
        return false;
    }
}