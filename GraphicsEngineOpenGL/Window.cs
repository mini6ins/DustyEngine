using DustyEngine.Components;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GraphicsEngineOpenGL;



public class RenderableObject
{
    public int VaoIndex;
    public Vector3 Position;
    public Vector3 Scale = Vector3.One;
    public Vector3 RotationEuler;
}

public class Window : GameWindow
{
    private float frameTime = 0.0f;
    private int fps = 0;
    public string NameExampleWindow { private set; get; }

    private Camera camera;
    private Vector2 lastMousePos;
    private bool firstMouseMove = true;
    
    
    private Matrix4 projection;

    private ShaderProgram shaderProgram;
    private readonly List<VAOManager> vaoList = new();
    private readonly List<RenderableObject> sceneObjects = new();

    public Window(GameWindowSettings gws, NativeWindowSettings nws, List<Mesh> meshes)
        : base(gws, nws)
    {
        NameExampleWindow = "OpenTK";
        Title = NameExampleWindow;

        Console.WriteLine(GL.GetString(StringName.Version));
        Console.WriteLine(GL.GetString(StringName.Vendor));
        Console.WriteLine(GL.GetString(StringName.Renderer));
        Console.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));

        VSync = VSyncMode.On;
        shaderProgram = new ShaderProgram(
            "C:\\Users\\maksym\\Documents\\GitHub\\DustyEngine\\DustyEngine\\Project\\shaders\\shader.vert",
            "C:\\Users\\maksym\\Documents\\GitHub\\DustyEngine\\DustyEngine\\Project\\shaders\\shader.frag");

        foreach (var mesh in meshes)
        {
            var vao = new VAOManager(shaderProgram);
            vao.CreateVAO(mesh.Vertices, mesh.Indices);
            vaoList.Add(vao);
        }
        for (int i = 0; i < vaoList.Count; i++)
        {
            sceneObjects.Add(new RenderableObject
            {
                VaoIndex = i,
                Position = new Vector3(i * 0.8f - 3, 0, 0f),
                Scale = Vector3.One * 0.01f,
                RotationEuler = Vector3.Zero
            });
        }
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);


        camera = new Camera(new Vector3(2f, 2f, 2f));
        CursorState = CursorState.Grabbed;
        
        projection =
            Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                Size.X / (float)Size.Y,
                0.1f,
                10000.0f
            );
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        var input = KeyboardState;

        frameTime += (float)args.Time;
        fps++;
        if (frameTime >= 1.0f)
        {
            Title = $"{NameExampleWindow} : FPS - {fps}";
            frameTime = 0.0f;
            fps = 0;
        }
        
        float deltaTime = (float)args.Time;

        Vector3 direction = Vector3.Zero;
        if (input.IsKeyDown(Keys.W)) direction += camera.Front;
        if (input.IsKeyDown(Keys.S)) direction -= camera.Front;
        if (input.IsKeyDown(Keys.A)) direction -= camera.Right;
        if (input.IsKeyDown(Keys.D)) direction += camera.Right;
        if (input.IsKeyDown(Keys.Space)) direction += camera.Up;
        if (input.IsKeyDown(Keys.LeftShift)) direction -= camera.Up;
        
        if (input.IsKeyPressed(Keys.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        if (input.IsKeyPressed(Keys.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        
        if (direction.LengthSquared > 0)
        {
            Vector3 newPosition = camera.Position + direction.Normalized() * 4 * deltaTime;
            camera.SetPosition(newPosition);
        }
    }
    
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (firstMouseMove)
        {
            lastMousePos = new Vector2(e.X, e.Y);
            firstMouseMove = false;
            return;
        }

        var delta = new Vector2(e.X, e.Y) - lastMousePos;
        lastMousePos = new Vector2(e.X, e.Y);

        camera.UpdateRotation(delta.X * 0.1f, delta.Y * 0.1f);
    }

    

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(Color4.Blue);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shaderProgram.ActiveProgram();
        shaderProgram.SetUniform("uView", camera.GetViewMatrix());

        shaderProgram.SetUniform("uProjection", projection);

        foreach (var obj in sceneObjects)
        {
            RenderObject(obj);
        }

        shaderProgram.DeactiveProgram();
        SwapBuffers();
    }

    private void RenderObject(RenderableObject obj)
    {
        Matrix4 rotation =
            Matrix4.CreateRotationX(obj.RotationEuler.X) *
            Matrix4.CreateRotationY(obj.RotationEuler.Y) *
            Matrix4.CreateRotationZ(obj.RotationEuler.Z);

        Matrix4 modelMatrix = Matrix4.CreateScale(obj.Scale) * rotation * Matrix4.CreateTranslation(obj.Position);
        shaderProgram.SetUniform("uModel", modelMatrix);
        vaoList[obj.VaoIndex].RenderVAO(0);
    }

    protected override void OnUnload()
    {
        foreach (var vao in vaoList)
            vao.Dispose();

        shaderProgram.DeleteProgram();
        base.OnUnload();
    }
}