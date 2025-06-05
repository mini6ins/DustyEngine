using DustyEngine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpneGL;

public class GraphicsEngineOpenGl
{
    public void RunMainLoop(DustyEngine.Scene.Scene scene, Action updateCallback, Vector2 resolution,
        string programName)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);
        
        var nativeWindowSettings = new NativeWindowSettings()
        {
            Size = new Vector2i((int)resolution.X, (int)resolution.Y),
            Title = programName,
        };

        using var window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);

        //   ResourceMonitor.Init();

        window.UpdateFrame += (e) =>
        {
            updateCallback?.Invoke();
            //     ResourceMonitor.Update();
        };
        window.Run();
    }
}