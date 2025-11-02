using System;
using System.Threading;
using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl : IRenderer
{
    private Thread? _threadA;
    private Thread? _threadB;

    private Window? _windowA;
    private Window? _windowB;

    public void RunMainLoop(Scene scene, Action updateCallback, Vector2i resolution, string programName,
        string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        // Поток №1 — первое окно
        _threadA = new Thread(() =>
        {
            RunWindow(scene, updateCallback, resolution, $"{programName} [Window A]",
                vertShaderPath, fragShaderPath, vsync, renderMode, ref _windowA);
        });
        _threadA.IsBackground = true;
        _threadA.Start();

        // Поток №2 — второе окно
        _threadB = new Thread(() =>
        {
            RunWindow(scene, updateCallback, resolution, $"{programName} [Window B]",
                vertShaderPath, fragShaderPath, vsync, renderMode, ref _windowB);
        });
        _threadB.IsBackground = true;
        _threadB.Start();

        // Можно ожидать завершения обоих окон (по желанию)
        _threadA.Join();
        _threadB.Join();
    }

    private void RunWindow(Scene scene, Action updateCallback, Vector2i resolution, string title,
        string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode, ref Window? windowRef)
    {
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = resolution,
            Title = title,
        };

        windowRef = new Window(
            GameWindowSettings.Default,
            nativeWindowSettings,
            scene,
            vertShaderPath,
            fragShaderPath,
            title,
            vsync,
            CursorState.Normal,
            renderMode);

        windowRef.UpdateFrame += _ => updateCallback?.Invoke();

        windowRef.Run();
    }

    public void AddRenderer(MeshRenderer meshRenderer)
    {
        _windowA?.AddRenderer(meshRenderer);
        _windowB?.AddRenderer(meshRenderer);
    }

    public bool RemoveRenderer(int objectId)
    {
        bool resultA = _windowA?.RemoveRenderer(objectId) ?? false;
        bool resultB = _windowB?.RemoveRenderer(objectId) ?? false;
        return resultA || resultB;
    }
}
