using System.Diagnostics;
using System.Text.Json;
using GraphicsEngineOpenGL;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DustyEditor;

internal static class DustyEditor
{
    static void Main(string[] args)
    {
       // using var editor = new DustyEditorWindow();
       // editor.Run();
       
       var gameWindowSettings = GameWindowSettings.Default;
       var nativeWindowSettings = new NativeWindowSettings()
       {
           Size = new Vector2i(800, 600),
       };
       
       // Создаем FrameReceiver в главном коде
       using var frameReceiver = new FrameReceiver();
            
       // Подписываемся на события
       frameReceiver.OnConnected += () => Console.WriteLine("=== ПОДКЛЮЧЕН К СЕРВЕРУ ===");
       frameReceiver.OnConnectionLost += (reason) => Console.WriteLine($"Соединение потеряно: {reason}");
            
       // Создаем окно и передаем ему FrameReceiver
       using var window = new RenderWindow(gameWindowSettings, nativeWindowSettings, frameReceiver);
            
       // Подключаемся к серверу
       _ = frameReceiver.ConnectAsync();
            
       Console.WriteLine("=== КЛИЕНТ РЕНДЕРИНГА ===");
       Console.WriteLine("Получаю фреймбуфер от сервера...");
       Console.WriteLine("ESC - Выход");
       Console.WriteLine("========================");
            
       window.Run();
       
    }
}