using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Utils;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl
{
    public void RunMainLoop(Scene scene, Action updateCallback, Vector2 resolution,
        string programName)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        var nativeWindowSettings = new NativeWindowSettings()
        {
            Size = new Vector2i((int)resolution.X, (int)resolution.Y),
            Title = programName,
        };

        List<Mesh> meshes = new List<Mesh>();

        
        foreach (var obj in scene.GameObjects)
        {
            CollectMeshes(obj, meshes);
        }

        Console.WriteLine($"Total meshes: {meshes.Count}");

        // float[] vertices;
        // uint[] indices;
        // OBJModelLoader.LoadModel("C:\\Users\\maksym\\Documents\\GitHub\\DustyEngine\\GraphicsEngine_OpenGL\\TeddyBear.obj", out vertices, out indices);
        //   
        // meshes.Add(new Mesh(vertices, indices));

        Console.WriteLine($"Meshes: {meshes.Count}");
        using var window = new Window(GameWindowSettings.Default, nativeWindowSettings, meshes);

        window.UpdateFrame += (e) => { updateCallback?.Invoke(); };
        window.Run();
    }
    
    
    public static void CollectMeshes(GameObject obj, List<Mesh> meshes)
    {
        foreach (var component in obj.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                var mesh = meshRenderer.GetMesh();
                if (mesh != null)
                    meshes.Add(mesh);
            }
        }

        foreach (var child in obj.Children)
        {
            CollectMeshes(child, meshes);
        }
    }

}