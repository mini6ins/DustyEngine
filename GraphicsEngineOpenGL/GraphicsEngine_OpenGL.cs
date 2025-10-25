using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl
{
    private List<MeshRenderer> _allRenderers = [];
    private Window? _window;
    private FramebufferSenderMMF? _sender;
   // private FramebufferSenderTCP? _senderTcp; // Опционально, для совместимости

   public async Task RunMainLoop(Scene scene, Action updateCallback, Vector2 resolution, string programName,
       string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode, bool useMMF = true)
   {
       Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

       // ИГНОРИРУЕМ переданное resolution и используем фиксированное 16:9
       int contextWidth = 1280;
       int contextHeight = 720;

       var nativeWindowSettings = new NativeWindowSettings()
       {
           ClientSize = new Vector2i(contextWidth, contextHeight),  // ИЗМЕНЕНО!
           Title = programName,
       };

       foreach (var obj in scene.GameObjects)
       {
           SceneManager.CollectMeshRenderers(obj, _allRenderers);
       }

       Debug.Log($"Total Meshes: {_allRenderers.Count}", Debug.LogLevel.Info, true);

       // Инициализируем MMF sender с правильным разрешением
       _sender = new FramebufferSenderMMF(contextWidth, contextHeight, 30);  // ИЗМЕНЕНО!

       // Запускаем MMF (синхронно, так как это локальная операция)
       if (!_sender.Start())
       {
           Debug.Log("Failed to start FramebufferSenderMMF", Debug.LogLevel.Error, true);
       }
       else
       {
           Debug.Log($"FramebufferSenderMMF started: {contextWidth}x{contextHeight}", Debug.LogLevel.Info, true);

           // Подписываемся на события ввода
           _sender.OnInputEventReceived += HandleRemoteInput;
       }

       // Создаем окно в режиме Context для MMF отправки
       _window = new Window(GameWindowSettings.Default, nativeWindowSettings, _allRenderers, vertShaderPath,
           fragShaderPath, programName, SceneManager.FindCamera(scene),
           vsync, CursorState.Normal, renderMode, _sender);

       if (renderMode == RenderMode.Context)
           _window.IsVisible = false;

       // Подключаем обработчики событий
       _window.UpdateFrame += (e) => { updateCallback?.Invoke(); };

       _window.Run();
   }

    private void HandleRemoteInput(FramebufferSenderMMF.InputEvent inputEvent)
    {
        // Здесь можно обработать входящие события от удаленного клиента
        switch (inputEvent.Type)
        {
            case FramebufferSenderMMF.InputEventType.KeyDown:
                Debug.Log($"Remote KeyDown: {inputEvent.KeyCode}", Debug.LogLevel.Info, true);
                // Передаем событие в Input систему или обрабатываем напрямую
                break;

            case FramebufferSenderMMF.InputEventType.KeyUp:
                Debug.Log($"Remote KeyUp: {inputEvent.KeyCode}", Debug.LogLevel.Info, true);
                break;

            case FramebufferSenderMMF.InputEventType.MouseMove:
                Debug.Log($"Remote MouseMove: ({inputEvent.MouseX}, {inputEvent.MouseY})", Debug.LogLevel.Info, true);
                break;

            case FramebufferSenderMMF.InputEventType.MouseDown:
                Debug.Log($"Remote MouseDown: Button {inputEvent.MouseButton}", Debug.LogLevel.Info, true);
                break;

            case FramebufferSenderMMF.InputEventType.MouseUp:
                Debug.Log($"Remote MouseUp: Button {inputEvent.MouseButton}", Debug.LogLevel.Info, true);
                break;

            case FramebufferSenderMMF.InputEventType.MouseWheel:
                Debug.Log($"Remote MouseWheel: {inputEvent.WheelDelta}", Debug.LogLevel.Info, true);
                break;
        }
    }

    public void AddRenderer(MeshRenderer meshRenderer) => _window?.AddRenderer(meshRenderer);

    public bool RemoveRenderer(int objectId) => _window?.RemoveRenderer(objectId) ?? false;

    public void Dispose()
    {
        _sender?.Dispose();
    }
}