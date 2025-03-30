using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace DustyEngine.GraphicsEngineOpneGL;

public class GraphicsEngineOpenGl
{
    public void RunMainLoop(Action updateCallback)
    {
        Console.WriteLine("GraphicsEngineOpenGl is working");
        var nativeWindowSettings = new NativeWindowSettings()
        {
            Size = new Vector2i((int)Program.settings.sceneSize.X,(int) Program.settings.sceneSize.Y),
            Title = Program.settings.ProjectName,
        };

        using var window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);
        
        ResourceMonitor.Init();
        
        window.UpdateFrame += (e) =>
        {
            updateCallback?.Invoke();
            ResourceMonitor.Update();
        };
        window.Run();
    }
}