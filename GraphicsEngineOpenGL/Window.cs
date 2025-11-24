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

    private readonly List<Camera> _sceneCameras;
    private readonly EditorCamera _editorCamera;
    private CameraBase ActiveCamera => ((_renderMode == RenderMode.Editor) && _editorCamera != null) ? _editorCamera : _sceneCameras.First();

    private Matrix4 _projection;
    
    private readonly CursorState _cursorState;
    private readonly RenderMode _renderMode;

    private readonly ShaderProgram _shaderProgram;
    private readonly List<VAOManager> _vaoList = [];
    private readonly List<RenderableObject> _sceneObjects = [];
    
    private readonly List<MeshRenderer> _allRenderers = [];

    private bool _initialized;

    public Window(GameWindowSettings gws, NativeWindowSettings nws, Scene scene,
        string vertShaderPath, string fragShaderPath, string windowName, bool isVsync = true, CursorState cursorState = CursorState.Normal,
        RenderMode renderMode = RenderMode.Editor)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        
        _cursorState = cursorState;
        _renderMode = renderMode;

        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        
        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);

        _sceneCameras = SceneManager.FindCameras();
        
        EditorCamera? editorCamera = null;
        if (renderMode == RenderMode.Editor)
        {
            var ec = new EditorCamera
            {
                AspectRatio = nws.ClientSize.X / nws.ClientSize.Y
            };

            ec.InternalTransform.LocalPosition = new Vector3(0f, 2.5f, 5f);
            ec.InternalTransform.LocalRotation = new Vector3(0f, 0f, 0f);

            _editorCamera = ec;
        }
        
        _allRenderers.Clear();
        foreach (var obj in scene.GameObjects) 
            SceneManager.CollectMeshRenderers(obj, _allRenderers);
        
        Debug.Log($"Total Meshes: {_allRenderers.Count}", Debug.LogLevel.Info, true);
        
        foreach (var meshRenderer in _allRenderers)
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

        _initialized = true;
    }

    private float _edYaw = 0f;
    private float _edPitch = 0f;
    private float _edSpeed = 8f;
    private float _edMouseSensitivity = 0.15f;
    private float _edSmoothDX = 0f;
    private float _edSmoothDY = 0f;
    private const float _edSmoothing = 0.3f;

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
            if (Input.IsMouseButtonDown(MouseButton.Middle))
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

            Input.ResetMouse();
        }

        if (Input.IsKeyDown(KeyCode.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        if (Input.IsKeyDown(KeyCode.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (_renderMode is RenderMode.Standalone or RenderMode.Editor) 
            Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderScene();

        SwapBuffers();
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

        _initialized = false;
        base.OnUnload();
    }
}