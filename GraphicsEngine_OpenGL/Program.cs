using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

namespace GraphicsEngine_OpenGL
{
    class Program
    {
        static void Main(string[] args)
        {
            var nativeWinSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(800, 600),
                Location = new Vector2i(370, 300),
                WindowBorder = WindowBorder.Resizable,
                WindowState = WindowState.Normal,


                // Flags = ContextFlags.ForwardCompatible,
                Flags = ContextFlags.Default,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Compatability,
                // Profile = ContextProfile.Core,
                API = ContextAPI.OpenGL,
                
                NumberOfSamples = 0
            };

         
            List<Mesh> meshes = new List<Mesh>();
            
            float[] vertices;
            uint[] indices;
            AssimpModelLoader.LoadModel("C:\\Users\\maksym\\Documents\\GitHub\\DustyEngine\\GraphicsEngine_OpenGL\\TeddyBear.obj", out vertices, out indices);
          
            meshes.Add(new Mesh(vertices, indices));
            meshes.Add(new Mesh(vertices, indices));
            meshes.Add(new Mesh(vertices, indices));
            meshes.Add(new Mesh(vertices, indices));
            meshes.Add(new Mesh(vertices, indices));
            meshes.Add(new Mesh(vertices, indices));
            meshes.Add(new Mesh(vertices, indices));
            
            
            using (ExampleWindow game = new ExampleWindow(GameWindowSettings.Default, nativeWinSettings, meshes))
            {
                game.Run();
            }
        }
    }
}