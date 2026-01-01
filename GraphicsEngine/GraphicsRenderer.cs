using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngine.RenderUtils;
using InputSystem;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseButton = InputSystem.MouseButton;
using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace GraphicsEngine;

public enum RenderMode
{
    Standalone,
    EditorStop,
    EditorRun
}

public class RenderableObject
{
    public int VaoIndex;
    public uint GameObjectId;
    public uint MeshRendererId;
    public Transform Transform = new();
    public MeshRenderer MeshRenderer = null!;
}

public class GraphicsRenderer(
    string vertShaderPath,
    string fragShaderPath,
    int viewportWidth,
    int viewportHeight,
    RenderMode initialRenderMode)
{
    private ShaderProgram _shaderProgram = null!;
    private readonly List<VAOManager> _vaoList = [];
    private readonly List<RenderableObject> _sceneObjects = [];

    public List<Camera> _sceneCameras = null!;

    private CameraBase? _activeCamera;
    private RenderMode _currentRenderMode = initialRenderMode;

    public CameraBase ActiveCamera
    {
        get
        {
            if (_activeCamera != null)
                return _activeCamera;

            if (_currentRenderMode == RenderMode.EditorStop && EditorCamera != null)
                return EditorCamera;

            if (_sceneCameras.Count > 0)
                return _sceneCameras.First();

            return EditorCamera!;
        }
        set => _activeCamera = value;
    }

    public EditorCamera? EditorCamera;
    private Matrix4 _projection;

    public int ViewportTexture { get; private set; }
    private int _viewportFramebuffer;
    private int _viewportDepthBuffer;
    private int _currentViewportWidth;
    private int _currentViewportHeight;

    public void SetRenderMode(RenderMode mode)
    {
        _currentRenderMode = mode;
        Debug.Log($"[GraphicsRenderer] Render mode set to: {mode}", Debug.LogLevel.Info, true);
    }

    public void Load()
    {
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);

        if (_currentRenderMode == RenderMode.EditorStop)
        {
            EditorCamera = new EditorCamera
            {
                AspectRatio = viewportWidth / (float)viewportHeight,
                InternalTransform =
                {
                    LocalPosition = new Vector3(0f, 2.5f, 5f),
                    LocalRotation = new Vector3(0f, 0f, 0f)
                }
            };

            EditorCamera.InitializeController();
            Input.EnableRpcInput();
            Debug.Log("RPC input mode enabled for Editor", Debug.LogLevel.Info, true);
        }

        CreateViewportFramebuffer();
        LoadScene();
    }

    public void LoadScene()
    {
        Debug.Log("[LoadScene] Loading scene", Debug.LogLevel.Info, true);

        foreach (var vao in _vaoList)
        {
            vao.Dispose();
        }

        _vaoList.Clear();
        _sceneObjects.Clear();

        _sceneCameras = SceneManager.FindCameras();
        Debug.Log($"[LoadScene] Found {_sceneCameras?.Count ?? 0} cameras in scene", Debug.LogLevel.Info, true);

        if (EditorCamera != null || _sceneCameras is { Count: > 0 })
        {
            ActiveCamera.AspectRatio = _currentViewportWidth / (float)_currentViewportHeight;
            _projection = ActiveCamera.GetProjectionMatrix();
        }
        else
        {
            Debug.Log("No cameras found in scene!", Debug.LogLevel.Warning, true);
        }

        LoadSceneRenderers();

        Debug.Log($"[LoadScene] Scene loaded successfully. Total renderers: {_sceneObjects.Count}", Debug.LogLevel.Info, true);
    }

    private void CreateViewportFramebuffer()
    {
        _currentViewportWidth = viewportWidth;
        _currentViewportHeight = viewportHeight;

        _viewportFramebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _viewportFramebuffer);

        ViewportTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, ViewportTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, viewportWidth, viewportHeight, 0, PixelFormat.Rgb,
            PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2d, ViewportTexture, 0);

        _viewportDepthBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _viewportDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, viewportWidth,
            viewportHeight);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
            RenderbufferTarget.Renderbuffer, _viewportDepthBuffer);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.FramebufferComplete)
            Debug.Log($"Framebuffer is not complete! Status: {status}", Debug.LogLevel.Error, true);
        else
            Debug.Log($"Framebuffer created successfully: {viewportWidth}x{viewportHeight}", Debug.LogLevel.Info, true);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void ResizeViewport(int width, int height)
    {
        if (width == _currentViewportWidth && height == _currentViewportHeight) return;
        if (width <= 0 || height <= 0) return;

        _currentViewportWidth = width;
        _currentViewportHeight = height;

        GL.BindTexture(TextureTarget.Texture2d, ViewportTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb,
            PixelType.UnsignedByte, IntPtr.Zero);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _viewportDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, width, height);

        ActiveCamera.AspectRatio = width / (float)height;
        _projection = ActiveCamera.GetProjectionMatrix();

        Debug.Log($"Viewport resized to: {width}x{height}", Debug.LogLevel.Info, true);
    }

    private void LoadSceneRenderers()
    {
        var allRenderers = new List<MeshRenderer>();

        if (SceneManager.CurrentScene == null) return;

        foreach (var obj in SceneManager.CurrentScene.GameObjects)
            SceneManager.CollectMeshRenderers(obj, allRenderers);

        Debug.Log($"Total Meshes: {allRenderers.Count}", Debug.LogLevel.Info, true);

        foreach (var meshRenderer in allRenderers)
            AddRenderer(meshRenderer);
    }

    public void Update(float deltaTime, KeyboardState keyboardState, MouseState mouseState)
    {
        switch (_currentRenderMode)
        {
            case RenderMode.EditorRun:
            case RenderMode.Standalone:
                Input.Update(keyboardState);
                Input.UpdateMouseState(mouseState);
                break;

            case RenderMode.EditorStop:
                UpdateEditorCamera(deltaTime);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        HandleDebugInput();
    }

    public void OnMouseMove(float x, float y)
    {
        if (_currentRenderMode is RenderMode.EditorRun or RenderMode.Standalone)
            Input.UpdateMouse(x, y);
    }

    private void UpdateEditorCamera(float deltaTime)
    {
        if (EditorCamera == null) return;

        var isMiddleMouseDown = Input.IsMouseButtonDown(MouseButton.Middle);
        var mouseDelta = Input.Delta;

        var movementInput = new MovementInput
        {
            Forward = Input.IsKeyDown(KeyCode.W),
            Backward = Input.IsKeyDown(KeyCode.S),
            Left = Input.IsKeyDown(KeyCode.A),
            Right = Input.IsKeyDown(KeyCode.D),
            Up = Input.IsKeyDown(KeyCode.Space),
            Down = Input.IsKeyDown(KeyCode.LeftShift)
        };

        EditorCamera.UpdateMovement(deltaTime, isMiddleMouseDown, mouseDelta, movementInput);

        if (Input.IsRpcInputActive)
            Input.RpcResetMouseDelta();
        else
            Input.ResetMouse();
    }

    private static void HandleDebugInput()
    {
        if (Input.IsKeyJustActivatedOnce(KeyCode.F1))
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

        if (Input.IsKeyJustActivatedOnce(KeyCode.F2))
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    public void Render()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _viewportFramebuffer);
        GL.Viewport(0, 0, _currentViewportWidth, _currentViewportHeight);

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);

        _shaderProgram.ActiveProgram();
        var viewMatrix = ActiveCamera.GetViewMatrix();
        _shaderProgram.SetUniform("uView", viewMatrix);
        _shaderProgram.SetUniform("uProjection", _projection);

        foreach (var obj in _sceneObjects.Where(obj => obj.MeshRenderer.IsActiveAndEnabled))
            RenderObject(obj);

        _shaderProgram.DeactiveProgram();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void PresentToScreen(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0) return;

        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _viewportFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

        GL.BlitFramebuffer(
            0, 0, _currentViewportWidth, _currentViewportHeight,
            0, 0, screenWidth, screenHeight,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Linear);

        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
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

    public void AddRenderer(MeshRenderer? meshRenderer)
    {
        if (meshRenderer == null)
        {
            Debug.Log("[AddRenderer] meshRenderer == null – skipping.");
            return;
        }

        meshRenderer.EnsureLoaded();
        var mesh = meshRenderer.GetMesh();

        if (mesh == null || mesh.Vertices.Length == 0 || mesh.Indices.Length == 0)
        {
            Debug.Log("[AddRenderer] Empty Mesh – skipping.");
            return;
        }

        if (meshRenderer.Parent == null)
        {
            Debug.Log("[AddRenderer] meshRenderer.Parent == null – skipping.");
            return;
        }

        var transform = meshRenderer.Parent.GetComponent<Transform>();
        if (transform == null)
        {
            Debug.Log("[AddRenderer] No Transform found on parent – skipping.");
            return;
        }

        var vao = new VAOManager(_shaderProgram);
        vao.CreateVAO(mesh.Vertices, mesh.Indices);
        _vaoList.Add(vao);

        var renderableObject = new RenderableObject
        {
            VaoIndex = _vaoList.Count - 1,
            GameObjectId = transform.GameObject!.Id,
            MeshRendererId = meshRenderer.Id,
            Transform = transform,
            MeshRenderer = meshRenderer,
        };

        _sceneObjects.Add(renderableObject);
        Debug.Log(
            $"Added RenderableObject - GameObject ID: {renderableObject.GameObjectId}, MeshRenderer ID: {renderableObject.MeshRendererId}",
            Debug.LogLevel.Info, true);
    }

    private int FindRenderableIndexByMeshRendererId(uint meshRendererId)
    {
        for (var i = 0; i < _sceneObjects.Count; i++)
            if (_sceneObjects[i].MeshRendererId == meshRendererId)
                return i;

        return -1;
    }

    public bool RemoveRendererByComponent(MeshRenderer? meshRenderer)
    {
        if (meshRenderer == null) return false;

        var idx = FindRenderableIndexByMeshRendererId(meshRenderer.Id);

        if (idx < 0)
        {
            Debug.Log($"[RemoveRendererByComponent] MeshRenderer with ID {meshRenderer.Id} not found in scene.",
                Debug.LogLevel.Warning);
            return false;
        }

        var obj = _sceneObjects[idx];

        if ((uint)obj.VaoIndex < (uint)_vaoList.Count)
        {
            _vaoList[obj.VaoIndex].Dispose();
            _vaoList.RemoveAt(obj.VaoIndex);

            foreach (var renderableObject in _sceneObjects.Where(t => t.VaoIndex > obj.VaoIndex))
                renderableObject.VaoIndex--;
        }

        _sceneObjects.RemoveAt(idx);
        Debug.Log($"[RemoveRendererByComponent] Successfully removed MeshRenderer ID: {meshRenderer.Id}");
        return true;
    }

    public void Dispose()
    {
        if (_currentRenderMode == RenderMode.EditorStop)
            Input.DisableRpcInput();

        if (_viewportFramebuffer != 0)
            GL.DeleteFramebuffer(_viewportFramebuffer);
        if (ViewportTexture != 0)
            GL.DeleteTexture(ViewportTexture);
        if (_viewportDepthBuffer != 0)
            GL.DeleteRenderbuffer(_viewportDepthBuffer);

        foreach (var vao in _vaoList)
            vao.Dispose();

        _shaderProgram?.DeleteProgram();
    }
}
