using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngineOpenGL.RenderUtils;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Utils;
using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace GraphicsEngineOpenGL;

public class RenderableObject
{
    public int VaoIndex;
    public Transform Transform = new();
    public MeshRenderer MeshRenderer = null!;
}

public enum RenderMode
{
    Standalone,
    Editor
}

public class Window : GameWindow
{
    private readonly string _windowName;

    private readonly List<Camera> _sceneCameras = null!;
    private readonly EditorCamera? _editorCamera;

    private CameraBase ActiveCamera => ((_renderMode == RenderMode.Editor) && _editorCamera != null)
        ? _editorCamera
        : _sceneCameras.First();

    private Matrix4 _projection;

    private readonly CursorState _cursorState;
    private readonly RenderMode _renderMode;

    private readonly ShaderProgram _shaderProgram;
    private readonly List<VAOManager> _vaoList = [];

    private readonly List<RenderableObject> _sceneObjects = [];
    private readonly List<MeshRenderer> _allRenderers = [];

    private bool _initialized;

    // RPC manager
    private RpcController? _rpcManager;


    public Window(GameWindowSettings gws, NativeWindowSettings nws, Scene scene,
        string vertShaderPath, string fragShaderPath, string windowName, bool isVsync = true,
        CursorState cursorState = CursorState.Normal, RenderMode renderMode = RenderMode.Editor)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        _cursorState = cursorState;
        _renderMode = renderMode;
        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);
        _sceneCameras = SceneManager.FindCameras();

        if (renderMode == RenderMode.Editor)
        {
            var ec = new EditorCamera
            {
                AspectRatio = nws.ClientSize.X / (float)nws.ClientSize.Y,
                InternalTransform =
                {
                    LocalPosition = new Vector3(0f, 2.5f, 5f),
                    LocalRotation = new Vector3(0f, 0f, 0f)
                }
            };
            _editorCamera = ec;
        }

        _allRenderers.Clear();
        foreach (var obj in scene.GameObjects)
            SceneManager.CollectMeshRenderers(obj, _allRenderers);

        Debug.Log($"Total Meshes: {_allRenderers.Count}", Debug.LogLevel.Info, true);

        foreach (var meshRenderer in _allRenderers)
            AddRenderer(meshRenderer);

        if (_renderMode == RenderMode.Editor)
        {
            _rpcManager = new RpcController(nws.ClientSize.X, nws.ClientSize.Y);
        }
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

        if (mesh == null || mesh.Vertices.Length == 0 || mesh.Indices.Length == 0)
        {
            Debug.Log("[AddRenderer] Empty Mesh — skipping.");
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
            _editorCamera?.InitializeController();
            _rpcManager?.Start();
            Input.Input.EnableRpcInput();
            Console.WriteLine("[Input] RPC input mode enabled for Editor");
        }

        _initialized = true;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (_renderMode != RenderMode.Editor)
        {
            Input.Input.Update(KeyboardState);
            Input.Input.UpdateMouseState(MouseState);
        }
        else
        {
            UpdateEditorCamera((float)args.Time);
        }

        HandleDebugKeys();
    }

    private void UpdateEditorCamera(float deltaTime)
    {
        if (_editorCamera == null) return;

        var isMiddleMouseDown = Input.Input.IsMouseButtonDown(MouseButton.Middle);
        var mouseDelta = Input.Input.Delta;

        var movementInput = new MovementInput
        {
            Forward = Input.Input.IsKeyDown(KeyCode.W),
            Backward = Input.Input.IsKeyDown(KeyCode.S),
            Left = Input.Input.IsKeyDown(KeyCode.A),
            Right = Input.Input.IsKeyDown(KeyCode.D),
            Up = Input.Input.IsKeyDown(KeyCode.Space),
            Down = Input.Input.IsKeyDown(KeyCode.LeftShift)
        };

        _editorCamera.UpdateMovement(deltaTime, isMiddleMouseDown, mouseDelta, movementInput);

        // Reset mouse delta after camera update
        if (Input.Input.IsRpcInputActive)
            Input.Input.RpcResetMouseDelta();
        else
            Input.Input.ResetMouse();
    }

    private static void HandleDebugKeys()
    {
        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F1))
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F2))
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (_renderMode == RenderMode.Standalone)
            Input.Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderScene();

        if (_renderMode == RenderMode.Editor && _rpcManager?.ConnectedClients > 0)
        {
            _rpcManager.CaptureFrame(FramebufferSize.X, FramebufferSize.Y);
        }

        SwapBuffers();
    }

    private void RenderScene()
    {
        _shaderProgram.ActiveProgram();
        var viewMatrix = ActiveCamera.GetViewMatrix();
        _shaderProgram.SetUniform("uView", viewMatrix);
        _shaderProgram.SetUniform("uProjection", _projection);

        foreach (var obj in _sceneObjects.Where(obj => obj.MeshRenderer.IsActiveAndEnabled))
            RenderObject(obj);

        _shaderProgram.DeactiveProgram();
    }

    private void RenderObject(RenderableObject obj)
    {
        var transform = obj.Transform;
        var rotation =
            Matrix4.CreateRotationX(transform.GlobalRotation.X) *
            Matrix4.CreateRotationY(transform.GlobalRotation.Y) *
            Matrix4.CreateRotationZ(transform.GlobalRotation.Z);
        var modelMatrix =
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

        _rpcManager?.Dispose();

        if (_renderMode == RenderMode.Editor)
        {
            Input.Input.DisableRpcInput();
        }

        foreach (var vao in _vaoList)
            vao.Dispose();
        _shaderProgram.DeleteProgram();
        _initialized = false;
        base.OnUnload();
    }
}