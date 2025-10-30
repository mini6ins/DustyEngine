using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Utils;
using V3 = DustyEngine.Engine.Math.Vectors.Vector3;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl
{
    private readonly List<MeshRenderer> _allRenderers = [];
    private Window? _window;
    
    public void RunMainLoop(Scene scene, Action updateCallback, Vector2i resolution, string programName,
        string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        int contextWidth = 1280;
        int contextHeight = 720;

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(resolution.X, resolution.Y),
            Title = programName,
        };

        _allRenderers.Clear();
        foreach (var obj in scene.GameObjects) SceneManager.CollectMeshRenderers(obj, _allRenderers);

        Debug.Log($"Total Meshes: {_allRenderers.Count}", Debug.LogLevel.Info, true);
        
        
        Camera sceneCamera = SceneManager.FindCamera(scene);

        EditorCamera? editorCamera = null;
        if ( renderMode == RenderMode.Editor)
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
            renderMode
        );
        
         if (renderMode == RenderMode.Editor)
        {
            _window.IsVisible = true;
            Debug.Log("Embedded mode: ImGui will render over OpenGL scene", Debug.LogLevel.Info, true);
        }

        _window.UpdateFrame += _ => { updateCallback?.Invoke(); };

        _window.Run();
    }
    

    public void AddRenderer(MeshRenderer meshRenderer) => _window?.AddRenderer(meshRenderer);
    public bool RemoveRenderer(int objectId) => _window?.RemoveRenderer(objectId) ?? false;
}