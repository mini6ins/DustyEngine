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

    public void RunMainLoop(Scene scene, Action updateCallback, Vector2 resolution, string programName,
        string vertShaderPath, string fragShaderPath, bool vsync)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i((int)resolution.X, (int)resolution.Y),
            Title = programName,
        };

        foreach (var obj in scene.GameObjects)
        {
            SceneManager.CollectMeshRenderers(obj, _allRenderers);
        }

        Debug.Log($"Total Meshes: {_allRenderers.Count}", Debug.LogLevel.Info, true);
        
        //var cursorState = runInEditor ? CursorState.Normal : CursorState.Grabbed;

        _window = new Window(GameWindowSettings.Default, nativeWindowSettings, _allRenderers, vertShaderPath,
            fragShaderPath, programName, SceneManager.FindCamera(scene),
            vsync, CursorState.Normal, RenderMode.Context);

        _window.UpdateFrame += (e) => updateCallback?.Invoke();

        _window.Run();
    }

    

    public void AddRenderer(MeshRenderer meshRenderer) => _window?.AddRenderer(meshRenderer);
    
    public bool RemoveRenderer(int objectId) => _window?.RemoveRenderer(objectId) ?? false;
}