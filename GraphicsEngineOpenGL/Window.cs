using DustyEngine;
using DustyEngine.Components;
using GraphicsEngineOpenGL.RenderUtils;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Utils;
using Vector2 = System.Numerics.Vector2;
using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;

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
    Editor
}

public class Window : GameWindow
{
    private readonly string _windowName;

    private Camera _camera;
    private EditorCamera _editorCamera;
    private CameraBase ActiveCamera => ((_renderMode == RenderMode.Editor) && _editorCamera != null) ? _editorCamera : _camera;

    private Matrix4 _projection;
    
    private CursorState _cursorState;
    private RenderMode _renderMode;

    private ShaderProgram _shaderProgram;
    private readonly List<VAOManager> _vaoList = [];
    private readonly List<RenderableObject> _sceneObjects = [];

    
    private int _sceneFramebuffer;
    private int _sceneColorTexture;
    private int _sceneDepthTexture;
    private int _sceneWidth = 1280;
    private int _sceneHeight = 720;

    private bool _initialized;

    private ImGuiManager? _imguiManager;
    private bool _showDemoWindow = true;
    private bool _showSceneStats = true;
    private bool _showSceneViewport = true;

    public Window(GameWindowSettings gws, NativeWindowSettings nws, List<MeshRenderer> allRenderers,
        string vertShaderPath, string fragShaderPath, string windowName,
        Camera camera, EditorCamera editorCamera, bool isVsync = true, CursorState cursorState = CursorState.Normal,
        RenderMode renderMode = RenderMode.Editor)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        _camera = camera;
        _editorCamera = editorCamera;
        _cursorState = cursorState;
        _renderMode = renderMode;

        _sceneHeight = nws.ClientSize.Y;
        _sceneWidth = nws.ClientSize.X;

        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;

        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);

        foreach (var meshRenderer in allRenderers)
            AddRenderer(meshRenderer);
    }

    public void AddRenderer(MeshRenderer? meshRenderer)
    {
        if (meshRenderer == null)
        {
            Debug.Log("[AddRenderer] meshRenderer == null — skipping.");
            return;
        }

        meshRenderer.EnsureLoaded();

        var mesh = meshRenderer.GetMesh();
        if (mesh == null || mesh.Vertices == null || mesh.Indices == null ||
            mesh.Vertices.Length == 0 || mesh.Indices.Length == 0)
        {
            Debug.Log("[AddRenderer] Empty Mesh — skipping. " +
                      "Make sure MeshRenderer loads a valid mesh or assign a default one.");
            return;
        }

        if (meshRenderer.Parent == null)
        {
            Debug.Log("[AddRenderer] meshRenderer.Parent == null — skipping.");
            return;
        }

        var transform = meshRenderer.Parent.GetComponent<Transform>();
        if (transform == null)
        {
            Debug.Log("[AddRenderer] No Transform found on parent — skipping.");
            return;
        }

        var vao = new VAOManager(_shaderProgram);
        vao.CreateVAO(mesh.Vertices, mesh.Indices);
        _vaoList.Add(vao);

        _sceneObjects.Add(new RenderableObject
        {
            VaoIndex = _vaoList.Count - 1,
            Transform = transform,
            MeshRenderer = meshRenderer,
        });
    }

    public bool RemoveRenderer(int objectId)
    {
        if (objectId < 0 || objectId >= _sceneObjects.Count) return false;

        var obj = _sceneObjects[objectId];
        if (obj.VaoIndex < _vaoList.Count)
        {
            _vaoList[obj.VaoIndex].Dispose();
            _vaoList.RemoveAt(obj.VaoIndex);
            foreach (var t in _sceneObjects.Where(t => t.VaoIndex > obj.VaoIndex))
                t.VaoIndex--;
        }

        _sceneObjects.RemoveAt(objectId);
        return true;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        if ((_renderMode == RenderMode.Editor) && _editorCamera != null)
        {
            var euler = _editorCamera.InternalTransform.LocalRotationQuat.ToEulerAngles();
            _edPitch = euler.X * (180f / MathF.PI);
            _edYaw = euler.Y * (180f / MathF.PI);
        }

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        CursorState = _cursorState;
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        ActiveCamera.AspectRatio = Size.X / (float)Size.Y;
        _projection = ActiveCamera.GetProjectionMatrix();


        if (_renderMode == RenderMode.Editor)
        {
            SetupSceneFramebuffer();

            _imguiManager = new ImGuiManager();
            _imguiManager.Initialize(this);
            Debug.Log("ImGui initialized for Editor mode", Debug.LogLevel.Info, true);
        }

        _initialized = true;
    }


    private void SetupSceneFramebuffer()
    {
        _sceneFramebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _sceneFramebuffer);

        _sceneColorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _sceneColorTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8,
            _sceneWidth, _sceneHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte,
            IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2d, _sceneColorTexture, 0);

        _sceneDepthTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _sceneDepthTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24,
            _sceneWidth, _sceneHeight, 0, PixelFormat.DepthComponent, PixelType.Float,
            IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2d, _sceneDepthTexture, 0);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
            throw new Exception("Scene framebuffer не готов!");

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        ActiveCamera.AspectRatio = (float)_sceneWidth / _sceneHeight;
        _projection = ActiveCamera.GetProjectionMatrix();
    }


    private float _edYaw = 0f;
    private float _edPitch = 0f;
    private float _edSpeed = 8f;
    private float _edMouseSensitivity = 0.15f;
    private float _edSmoothDX = 0f;
    private float _edSmoothDY = 0f;
    private const float _edSmoothing = 0.3f;
    
    private bool _isViewportHovered = false;
    private bool _isViewportFocused = false;

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        Input.Update(KeyboardState);
        Input.UpdateMouseState(MouseState);
        
        EditorCameraMovement(args);
    }

    private void EditorCameraMovement(FrameEventArgs args)
    {
        float dt = (float)args.Time;

        if ((_renderMode == RenderMode.Editor) && _editorCamera is EditorCamera ec)
        {
            bool canControl = (_renderMode == RenderMode.Editor && _isViewportHovered);

            if (canControl && Input.IsMouseButtonDown(MouseButton.Middle))
            {
                (float dx, float dy) = Input.Delta;
                _edSmoothDX = _edSmoothDX * _edSmoothing + dx * (1f - _edSmoothing);
                _edSmoothDY = _edSmoothDY * _edSmoothing + dy * (1f - _edSmoothing);

                const float deadZone = 0.001f;
                if (System.Math.Abs(_edSmoothDX) > deadZone || System.Math.Abs(_edSmoothDY) > deadZone)
                {
                    _edYaw -= _edSmoothDX * _edMouseSensitivity;
                    _edPitch -= _edSmoothDY * _edMouseSensitivity;
                    _edPitch = System.Math.Clamp(_edPitch, -89f, 89f);

                    float pitchRad = _edPitch * (MathF.PI / 180f);
                    float yawRad = _edYaw * (MathF.PI / 180f);

                    var currentRight = ec.InternalTransform.LocalRotationQuat.Rotate(
                        new Vector3(1f, 0f, 0f)
                    );
                    var qPitch = Quaternion.FromAxisAngle(currentRight, pitchRad);

                    var localUp = qPitch.Rotate(new Vector3(0f, 1f, 0f));
                    var qYaw = Quaternion.FromAxisAngle(localUp, yawRad);

                    ec.InternalTransform.LocalRotationQuat = qYaw * qPitch;
                }
            }

            if (canControl)
            {
                var fwd = ec.InternalTransform.Forward;
                var right = ec.InternalTransform.Right;
                var up = ec.InternalTransform.Up;

                var dir = Vector3.Zero;
                if (Input.IsKeyDown(KeyCode.W)) dir += fwd;
                if (Input.IsKeyDown(KeyCode.S)) dir -= fwd;
                if (Input.IsKeyDown(KeyCode.A)) dir -= right;
                if (Input.IsKeyDown(KeyCode.D)) dir += right;
                if (Input.IsKeyDown(KeyCode.Space)) dir += up;
                if (Input.IsKeyDown(KeyCode.LeftShift)) dir -= up;

                if (dir.LengthSquared > 0f)
                {
                    dir = dir.Normalized();
                    ec.InternalTransform.LocalPosition += dir * _edSpeed * dt;
                }
            }

            Input.ResetMouse();
        }

        if (Input.IsKeyDown(KeyCode.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        if (Input.IsKeyDown(KeyCode.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (_renderMode is RenderMode.Standalone or RenderMode.Editor) Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        if (_renderMode == RenderMode.Editor)
            RenderEditor();
        else
            RenderStandalone();

        SwapBuffers();
    }


    private void RenderEditor()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _sceneFramebuffer);
        GL.Viewport(0, 0, _sceneWidth, _sceneHeight);

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderScene();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_imguiManager == null) return;
        _imguiManager.NewFrame();
        RenderImGuiInterface();
        _imguiManager.Render();
    }

    private void RenderImGuiInterface()
    {
        if (_showSceneViewport)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            if (ImGui.Begin("Scene Viewport", ref _showSceneViewport))
            {
                _isViewportHovered = ImGui.IsWindowHovered();
                _isViewportFocused = ImGui.IsWindowFocused();

                var viewportSize = ImGui.GetContentRegionAvail();


                const float targetAspect = 1280f / 720f;
                float availableAspect = viewportSize.X / viewportSize.Y;

                Vector2 imageSize;
                Vector2 padding = Vector2.Zero;

                if (availableAspect > targetAspect)
                {
                    imageSize = new Vector2(viewportSize.Y * targetAspect, viewportSize.Y);
                    padding = new Vector2((viewportSize.X - imageSize.X) * 0.5f, 0);
                }
                else
                {
                    imageSize = new Vector2(viewportSize.X, viewportSize.X / targetAspect);
                    padding = new Vector2(0, (viewportSize.Y - imageSize.Y) * 0.5f);
                }


                ImGui.SetCursorPos(ImGui.GetCursorPos() + padding);

                ImGui.Image(
                    (IntPtr)_sceneColorTexture,
                    imageSize,
                    new Vector2(0, 1),
                    new Vector2(1, 0)
                );
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }
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

        var viewMatrix = ActiveCamera.GetViewMatrix();
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

        foreach (var vao in _vaoList)
            vao.Dispose();

        _shaderProgram.DeleteProgram();

        if (_renderMode == RenderMode.Editor)
        {
            if (_sceneFramebuffer != 0)
            {
                GL.DeleteFramebuffer(_sceneFramebuffer);
                GL.DeleteTexture(_sceneColorTexture);
                GL.DeleteTexture(_sceneDepthTexture);
            }

            _imguiManager?.Shutdown();
        }

        _initialized = false;
        base.OnUnload();
    }
}