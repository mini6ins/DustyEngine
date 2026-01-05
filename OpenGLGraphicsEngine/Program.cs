using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace OpenGLGraphicsEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(600, 450),
                Title = "DustyEngine v0.1",
                APIVersion = new Version(4, 0),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.Default,
            };

            var meshComponents = CreateTestMeshList();


            using (Window mainWindow =
                   new Window(GameWindowSettings.Default, nativeWindowSettings, meshComponents, false))
            {
                mainWindow.Run();
            }
        }

        private static List<MeshComponent> CreateTestMeshList()
        {
            List<MeshComponent> meshComponents = new List<MeshComponent>();

            MeshComponent meshComponent = new MeshComponent();
            meshComponent.LoadFromObj("/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/CubeOBJ/cube.obj");
            meshComponent.Material.DiffuseTexturePath = "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/CubeOBJ/EmilyImage.jpg";
            meshComponent.Position = Vector3.Zero;
            meshComponents.Add(meshComponent);


            MeshComponent meshComponent2 = new MeshComponent();
            meshComponent2.LoadFromObj("/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/CubeOBJ/cube.obj");

            meshComponent2.Position = new Vector3(5, 0, 0);
            meshComponent2.Scale = new Vector3(1, 5, 2);
            meshComponent2.Material = new Material
            {
                DiffuseTexturePath = "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/CubeOBJ/image/metal.jpg",
            };

            meshComponents.Add(meshComponent2);


            MeshComponent meshComponent3 = new MeshComponent();
            meshComponent3.LoadFromObj("/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/CubeOBJ/cube.obj");

            meshComponent3.Position = new Vector3(-5, 0, 0);
            meshComponent3.Scale = new Vector3(1, 1, 1);
            meshComponent3.Material = new Material
            {
                DiffuseTexturePath = "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/CubeOBJ/image/normalmaptest.png",
                NormalTexturePath = "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/CubeOBJ/image/normalmap.png",
            };

            meshComponents.Add(meshComponent3);
            return meshComponents;
        }
    }
}