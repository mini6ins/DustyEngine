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
    public MeshRenderer MeshRenderer;
}

public enum RenderMode
{
    Standalone,
    Context
}

public class Window : GameWindow
{
    private float _frameTime;
    private int _fps;
    private readonly string _windowName;

    private Camera _camera;
    private Matrix4 _projection;
    private CursorState _cursorState;
    private RenderMode _renderMode;

    private ShaderProgram _shaderProgram;
    private readonly List<VAOManager> _vaoList = [];
    private readonly List<RenderableObject> _sceneObjects = [];


    private int _contextFramebuffer;
    private int _contextColorTexture;
    private int _contextDepthTexture;
    public static int ContextFramebufferWidth = 1280;
    public static int ContextFramebufferHeight = 720;

    private bool _initialized;

    private FramebufferSenderMMF? _framebufferSender;

    public Window(GameWindowSettings gws, NativeWindowSettings nws, List<MeshRenderer> allRenderers,
        string vertShaderPath, string fragShaderPath, string windowName,
        Camera camera, bool isVsync = true, CursorState cursorState = CursorState.Normal,
        RenderMode renderMode = RenderMode.Context, FramebufferSenderMMF? framebufferSenderMmf = null)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        _camera = camera;
        _cursorState = cursorState;
        _renderMode = renderMode;
        _framebufferSender = framebufferSenderMmf;

        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;

        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);

        foreach (var meshRenderer in allRenderers)
            AddRenderer(meshRenderer);
    }

    public int AddRenderer(MeshRenderer? meshRenderer)
    {
        var mesh = meshRenderer?.GetMesh();
        if (mesh?.Vertices == null) return -1;

        var vao = new VAOManager(_shaderProgram);
        vao.CreateVAO(mesh.Vertices, mesh.Indices);
        _vaoList.Add(vao);

        _sceneObjects.Add(new RenderableObject
        {
            VaoIndex = _vaoList.Count - 1,
            Transform = meshRenderer.Parent.GetComponent<Transform>(),
            MeshRenderer = meshRenderer,
        });
        return _sceneObjects.Count - 1;
    }

    public bool RemoveRenderer(int objectId)
    {
        if (objectId < 0 || objectId >= _sceneObjects.Count) return false;

        var obj = _sceneObjects[objectId];
        if (obj.VaoIndex < _vaoList.Count)
        {
            _vaoList[obj.VaoIndex].Dispose();
            _vaoList.RemoveAt(obj.VaoIndex);
            foreach (var t in _sceneObjects.Where(t => t.VaoIndex > obj.VaoIndex)) t.VaoIndex--;
        }

        _sceneObjects.RemoveAt(objectId);
        return true;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        CursorState = _cursorState;
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        _camera.AspectRatio = Size.X / (float)Size.Y;
        _projection = _camera.GetProjectionMatrix();

        if (_renderMode == RenderMode.Context)
        {
            SetupContextFramebuffer();
            
            Input.SetRemoteInputMode(true);

            _framebufferSender ??= new FramebufferSenderMMF(ContextFramebufferWidth, ContextFramebufferHeight, 60);
            if (!_framebufferSender.IsRunning)
            {
                if (_framebufferSender.Start())
                {
                    _framebufferSender.OnInputEventReceived += OnRemoteInputReceived;
                }
            }
        }

        _initialized = true;
    }

    private void SetupContextFramebuffer()
    {
        _contextFramebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _contextFramebuffer);

        _contextColorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _contextColorTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8,
            ContextFramebufferWidth, ContextFramebufferHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte,
            IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2d, _contextColorTexture, 0);

        _contextDepthTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _contextDepthTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24,
            ContextFramebufferWidth, ContextFramebufferHeight, 0, PixelFormat.DepthComponent, PixelType.Float,
            IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2d, _contextDepthTexture, 0);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
            throw new Exception("Context framebuffer не готов!");

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        _camera.AspectRatio = (float)ContextFramebufferWidth / ContextFramebufferHeight;
        _projection = _camera.GetProjectionMatrix();

        float aspect = (float)ContextFramebufferWidth / ContextFramebufferHeight;
        Console.WriteLine(
            $"[SERVER] Context framebuffer: {ContextFramebufferWidth}x{ContextFramebufferHeight}, aspect: {aspect:F3}");
    }
    
    private void OnRemoteInputReceived(FramebufferSenderMMF.InputEvent evt)
    {
        switch (evt.Type)
        {
            case FramebufferSenderMMF.InputEventType.KeyDown:
                Input.ProcessRemoteKeyEvent((OpenTK.Windowing.GraphicsLibraryFramework.Keys)evt.KeyCode, true);
                break;

            case FramebufferSenderMMF.InputEventType.KeyUp:
                Input.ProcessRemoteKeyEvent((OpenTK.Windowing.GraphicsLibraryFramework.Keys)evt.KeyCode, false);
                break;

            case FramebufferSenderMMF.InputEventType.MouseMove:
                Input.ProcessRemoteMouseMove(evt.MouseX, evt.MouseY);
                break;
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        if (_renderMode == RenderMode.Context)
            Input.Update();
        else
            Input.Update(KeyboardState);
        
        float deltaTime = (float)args.Time;

        _frameTime += deltaTime;
        _fps++;
        if (_frameTime >= 1.0f)
        {
            Title = $"{_windowName} : FPS - {_fps} | Objects: {_sceneObjects.Count} | Mode: {_renderMode}";
            _frameTime = 0.0f;
            _fps = 0;
        }

        if (Input.IsKeyDown(KeyCode.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        if (Input.IsKeyDown(KeyCode.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        if (_renderMode == RenderMode.Standalone)
            Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        if (_renderMode == RenderMode.Context) RenderToContext();
        else RenderStandalone();

        SwapBuffers();
    }

    private void RenderToContext()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _contextFramebuffer);
        GL.Viewport(0, 0, ContextFramebufferWidth, ContextFramebufferHeight);

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderScene();
        
        if (_framebufferSender?.IsRunning == true)
            _framebufferSender.SendFramebuffer(_contextFramebuffer, ContextFramebufferWidth, ContextFramebufferHeight,
                false);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }
    
    private void RenderStandalone()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderScene();
    }

    private void RenderScene()
    {
        _shaderProgram.ActiveProgram();

        var viewMatrix = _camera.GetViewMatrix();
        _shaderProgram.SetUniform("uView", viewMatrix);
        _shaderProgram.SetUniform("uProjection", _projection);

        foreach (var obj in _sceneObjects)
            if (obj.MeshRenderer.IsActiveAndEnabled)
                RenderObject(obj);

        _shaderProgram.DeactiveProgram();
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
            _vaoList[obj.VaoIndex].RenderVAO(0);
    }

    protected override void OnUnload()
    {
        if (!_initialized) return;
        
        if (_framebufferSender != null)
        {
            _framebufferSender.OnInputEventReceived -= OnRemoteInputReceived;
        }

        foreach (var vao in _vaoList) vao.Dispose();
        _shaderProgram.DeleteProgram();

        if (_renderMode == RenderMode.Context)
        {
            GL.DeleteFramebuffer(_contextFramebuffer);
            GL.DeleteTexture(_contextColorTexture);
            GL.DeleteTexture(_contextDepthTexture);

            _framebufferSender?.Dispose();
        }

        _initialized = false;
        base.OnUnload();
    }
}