using DustyEngine;
using DustyEngine.Components;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Utils;

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
    private Matrix4 projection;
    
    private CursorState _cursorState;
    
    private ShaderProgram shaderProgram;
    private readonly List<VAOManager> vaoList = new();
    private readonly List<RenderableObject> sceneObjects = new();

    public Window(GameWindowSettings gws, NativeWindowSettings nws, List<MeshRenderer> allRenderers, string windowName,
        Camera camera, bool isVsync = true, CursorState cursorState = CursorState.Normal)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        this.camera = camera;
        _cursorState = cursorState;
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
        Input.Update(KeyboardState);
        
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        CursorState = _cursorState;

        projection = camera.GetProjectionMatrix();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        float deltaTime = (float)args.Time;
        
        frameTime += deltaTime;
        fps++;
        if (frameTime >= 1.0f)
        {
            Title = $"{_windowName} : FPS - {fps}";
            frameTime = 0.0f;
            fps = 0;
        }
        
        //Debug
       if (Input.IsKeyDown(KeyCode.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
       if (Input.IsKeyDown(KeyCode.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
       if (Input.IsKeyDown(KeyCode.Escape)) Close();
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        Input.UpdateMouse(e.X, e.Y);
    }
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        shaderProgram.ActiveProgram();

        var viewMatrix = camera.GetViewMatrix();

        shaderProgram.SetUniform("uView", viewMatrix);
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


        if (obj.VaoIndex < vaoList.Count)
        {
            vaoList[obj.VaoIndex].RenderVAO(0);
        }
        else
        {
            Debug.Log($"Invalid VAO index: {obj.VaoIndex}", Debug.LogLevel.Error, true);
        }
    }

    protected override void OnUnload()
    {
        foreach (var vao in vaoList)
            vao.Dispose();

        shaderProgram.DeleteProgram();
        base.OnUnload();
    }
}