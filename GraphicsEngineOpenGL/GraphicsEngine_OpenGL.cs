using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Utils;
using V3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl
{
    private readonly List<MeshRenderer> _allRenderers = [];
    private Window? _window;
    private FramebufferSenderMMF? _sender;

    public Task RunMainLoop(Scene scene, Action updateCallback, Vector2 resolution, string programName,
        string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);
        
        int contextWidth = 1280;
        int contextHeight = 720;

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(contextWidth, contextHeight),
            Title = programName,
        };
        
        _allRenderers.Clear();
        foreach (var obj in scene.GameObjects)
            SceneManager.CollectMeshRenderers(obj, _allRenderers);

        Debug.Log($"Total Meshes: {_allRenderers.Count}", Debug.LogLevel.Info, true);
        
        _sender = new FramebufferSenderMMF(contextWidth, contextHeight, 200);
        if (!_sender.Start())
        {
            Debug.Log("Failed to start FramebufferSenderMMF", Debug.LogLevel.Error, true);
        }
        else
        {
            Debug.Log($"FramebufferSenderMMF started: {contextWidth}x{contextHeight}", Debug.LogLevel.Info, true);
            _sender.OnInputEventReceived += HandleRemoteInput;

            if (renderMode == RenderMode.Context)
            {
                Input.SetRemoteInputMode(true);
                Debug.Log("Remote input mode ENABLED", Debug.LogLevel.Info, true);
            }
        }
        
        Camera sceneCamera = SceneManager.FindCamera(scene);
        
        EditorCamera? editorCamera = null;
        if (renderMode == RenderMode.Context)
        {
            var ec = new EditorCamera
            {
                AspectRatio = contextWidth / (float)contextHeight
            };
            
            ec.InternalTransform.LocalPosition = new V3(0f, 2.5f, 5f);
            ec.InternalTransform.LocalRotation = new V3(0f, 0f, 0f); 

            editorCamera = ec;
        }

        _window = new Window(
            GameWindowSettings.Default,
            nativeWindowSettings,
            _allRenderers,
            vertShaderPath,
            fragShaderPath,
            programName,
            sceneCamera,  
            editorCamera, 
            vsync,
            CursorState.Normal,
            renderMode,
            _sender
        );

        if (renderMode == RenderMode.Context)
            _window.IsVisible = false;

        _window.UpdateFrame += _ => { updateCallback?.Invoke(); };

        _window.Run();
        return Task.CompletedTask;
    }

    private void HandleRemoteInput(MMFShared.InputEvent inputEvent)
    {
        var key = (Keys)inputEvent.KeyCode;
        var eventType = (MMFShared.InputEventType)inputEvent.Type;

        switch (eventType)
        {
            case MMFShared.InputEventType.KeyDown:
                Input.ProcessRemoteKeyEvent(key, true);
                Debug.Log($"Remote KeyDown: {key}", Debug.LogLevel.Info, true);
                break;
            case MMFShared.InputEventType.KeyUp:
                Input.ProcessRemoteKeyEvent(key, false);
                Debug.Log($"Remote KeyUp: {key}", Debug.LogLevel.Info, true);
                break;
            case MMFShared.InputEventType.MouseMove:
                Input.ProcessRemoteMouseMove(inputEvent.MouseX, inputEvent.MouseY);
                break;
            case MMFShared.InputEventType.MouseDown:
                Input.ProcessRemoteMouseButton((Utils.MouseButton)inputEvent.MouseButton, true);
                Debug.Log(
                    $"Remote MouseDown: Button {inputEvent.MouseButton} at ({inputEvent.MouseX:F2}, {inputEvent.MouseY:F2})",
                    Debug.LogLevel.Info, true);
                break;
            case MMFShared.InputEventType.MouseUp:
                Input.ProcessRemoteMouseButton((Utils.MouseButton)inputEvent.MouseButton, false);
                Debug.Log($"Remote MouseUp: Button {inputEvent.MouseButton}", Debug.LogLevel.Info, true);
                break;
            case MMFShared.InputEventType.MouseWheel:
                Debug.Log($"Remote MouseWheel: {inputEvent.WheelDelta:F2}", Debug.LogLevel.Info, true);
                break;
        }
    }

    public void AddRenderer(MeshRenderer meshRenderer) => _window?.AddRenderer(meshRenderer);
    public bool RemoveRenderer(int objectId) => _window?.RemoveRenderer(objectId) ?? false;
}
