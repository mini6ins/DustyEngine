using DustyEngine;
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
    public Transform Transform = new();
}

public class Window : GameWindow
{
    private float frameTime = 0.0f;
    private int fps = 0;
    private readonly string _windowName;

    private Camera camera;
    private Vector2 lastMousePos;
    private bool firstMouseMove = true;

    private Matrix4 projection;

    private ShaderProgram shaderProgram;
    private readonly List<VAOManager> vaoList = new();
    private readonly List<RenderableObject> sceneObjects = new();

    public Window(GameWindowSettings gws, NativeWindowSettings nws, List<MeshRenderer> allRenderers, string windowName,
        bool isVsync = true)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;

        Debug.Log(GL.GetString(StringName.Version), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.Vendor), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.Renderer), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.ShadingLanguageVersion), Debug.LogLevel.Info, true);

        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;

        shaderProgram = new ShaderProgram(
            "C:\\Users\\maksym\\Documents\\GitHub\\DustyEngine\\DustyEngine\\Project\\shaders\\shader.vert",
            "C:\\Users\\maksym\\Documents\\GitHub\\DustyEngine\\DustyEngine\\Project\\shaders\\shader.frag");

        foreach (var meshRenderer in allRenderers)
        {
            var vao = new VAOManager(shaderProgram);
            vao.CreateVAO(meshRenderer.GetMesh().Vertices, meshRenderer.GetMesh().Indices);
            vaoList.Add(vao);

            sceneObjects.Add(new RenderableObject
            {
                VaoIndex = vaoList.Count - 1,
                Transform = meshRenderer.Parent.GetComponent<Transform>()
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
            Title = $"{_windowName} : FPS - {fps}";
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
        if (KeyboardState.IsKeyDown(Keys.Escape)) Close(); 
        
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
        var transform = obj.Transform;

        Matrix4 rotation =
            Matrix4.CreateRotationX(transform.GlobalRotation.X) *
            Matrix4.CreateRotationY(transform.GlobalRotation.Y) *
            Matrix4.CreateRotationZ(transform.GlobalRotation.Z);

        Matrix4 modelMatrix =
            Matrix4.CreateScale(transform.GlobalScale.ToOpenTK()) *
            rotation *
            Matrix4.CreateTranslation(transform.GlobalPosition.ToOpenTK());

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