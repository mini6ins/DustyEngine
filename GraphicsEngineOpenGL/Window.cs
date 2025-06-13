using DustyEngine;
using DustyEngine.Components;
using GraphicsEngineOpenGL.RenderUtils;
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
    private float _frameTime;
    private int _fps;
    private readonly string _windowName;

    private Camera _camera;
    private Matrix4 _projection;

    private CursorState _cursorState;

    private ShaderProgram _shaderProgram;
    private readonly List<VAOManager> _vaoList = new();
    private readonly List<RenderableObject> _sceneObjects = new();

    public Window(GameWindowSettings gws, NativeWindowSettings nws, List<MeshRenderer> allRenderers,
        string vertShaderPath, string fragShaderPath, string windowName,
        Camera camera, bool isVsync = true, CursorState cursorState = CursorState.Normal)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        this._camera = camera;
        _cursorState = cursorState;
        Debug.Log(GL.GetString(StringName.Version), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.Vendor), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.Renderer), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.ShadingLanguageVersion), Debug.LogLevel.Info, true);

        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;

        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);

        foreach (var meshRenderer in allRenderers)
        {
            var vao = new VAOManager(_shaderProgram);
            vao.CreateVAO(meshRenderer.GetMesh().Vertices, meshRenderer.GetMesh().Indices);
            _vaoList.Add(vao);

            _sceneObjects.Add(new RenderableObject
            {
                VaoIndex = _vaoList.Count - 1,
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

        _projection = _camera.GetProjectionMatrix();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        Input.Update(KeyboardState);
        float deltaTime = (float)args.Time;

        _frameTime += deltaTime;
        _fps++;
        if (_frameTime >= 1.0f)
        {
            Title = $"{_windowName} : FPS - {_fps}";
            _frameTime = 0.0f;
            _fps = 0;
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

        _shaderProgram.ActiveProgram();

        var viewMatrix = _camera.GetViewMatrix();

        _shaderProgram.SetUniform("uView", viewMatrix);
        _shaderProgram.SetUniform("uProjection", _projection);


        foreach (var obj in _sceneObjects)
        {
            RenderObject(obj);
        }

        _shaderProgram.DeactiveProgram();
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

        _shaderProgram.SetUniform("uModel", modelMatrix);


        if (obj.VaoIndex < _vaoList.Count)
        {
            _vaoList[obj.VaoIndex].RenderVAO(0);
        }
        else
        {
            Debug.Log($"Invalid VAO index: {obj.VaoIndex}", Debug.LogLevel.Error, true);
        }
    }

    protected override void OnUnload()
    {
        foreach (var vao in _vaoList)
            vao.Dispose();

        _shaderProgram.DeleteProgram();
        base.OnUnload();
    }
}