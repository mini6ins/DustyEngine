using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngineOpenGL.RenderUtils;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Utils;
using MouseButton = Utils.MouseButton;
using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace GraphicsEngineOpenGL;

public class RenderableObject
{
    public int VaoIndex;
    public Transform Transform = new();
    public MeshRenderer MeshRenderer = null!;
}


public class GraphicsRenderer(string vertShaderPath, string fragShaderPath, int viewportWidth, int viewportHeight)
{
    private ShaderProgram _shaderProgram = null!;
    private readonly List<VAOManager> _vaoList = [];
    private readonly List<RenderableObject> _sceneObjects = [];

    private List<Camera> _sceneCameras = null!;
    private CameraBase ActiveCamera => IsEditorMode && _editorCamera != null ? _editorCamera : _sceneCameras.First();
    private EditorCamera? _editorCamera;
    private Matrix4 _projection;

    public int ViewportTexture { get; private set; }
    private int _viewportFramebuffer;
    private int _viewportDepthBuffer;
    private int _currentViewportWidth;
    private int _currentViewportHeight;

    private static bool IsEditorMode => GraphicsEngineOpenGl.RenderMode == RenderMode.Editor;

    public void Load()
    {
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);

        _sceneCameras = SceneManager.FindCameras();

        if (IsEditorMode)
        {
            _editorCamera = new EditorCamera
            {
                AspectRatio = viewportWidth / (float)viewportHeight,
                InternalTransform =
                {
                    LocalPosition = new Vector3(0f, 2.5f, 5f),
                    LocalRotation = new Vector3(0f, 0f, 0f)
                }
            };

            _editorCamera?.InitializeController();
            Input.Input.EnableRpcInput();
            Debug.Log("RPC input mode enabled for Editor", Debug.LogLevel.Info, true);
        }

        CreateViewportFramebuffer();

        if (IsEditorMode && _editorCamera != null || _sceneCameras is { Count: > 0 })
        {
            ActiveCamera.AspectRatio = viewportWidth / (float)viewportHeight;
            _projection = ActiveCamera.GetProjectionMatrix();
        }
        else
        {
            Debug.Log("No cameras found in scene!", Debug.LogLevel.Warning, true);
        }

        LoadSceneRenderers();
    }

    private void CreateViewportFramebuffer()
    {
        _currentViewportWidth = viewportWidth;
        _currentViewportHeight = viewportHeight;

        _viewportFramebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _viewportFramebuffer);

        ViewportTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, ViewportTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, viewportWidth, viewportHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, ViewportTexture, 0);

        _viewportDepthBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _viewportDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, viewportWidth, viewportHeight);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _viewportDepthBuffer);

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
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _viewportDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, width, height);

        if (_editorCamera != null)
        {
            _editorCamera.AspectRatio = width / (float)height;
            _projection = _editorCamera.GetProjectionMatrix();
        }
        else if (_sceneCameras.Count > 0)
        {
            ActiveCamera.AspectRatio = width / (float)height;
            _projection = ActiveCamera.GetProjectionMatrix();
        }

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
        if (!IsEditorMode)
        {
            Input.Input.Update(keyboardState);
            Input.Input.UpdateMouseState(mouseState);
        }
        else
            UpdateEditorCamera(deltaTime);

        HandleDebugInput();
    }

    public void OnMouseMove(float x, float y)
    {
        if (!IsEditorMode)
            Input.Input.UpdateMouse(x, y);
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

        if (Input.Input.IsRpcInputActive)
            Input.Input.RpcResetMouseDelta();
        else
            Input.Input.ResetMouse();
    }

    private static void HandleDebugInput()
    {
        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F1))
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F2))
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

    public void Dispose()
    {
        if (IsEditorMode)
        {
            Input.Input.DisableRpcInput();
        }

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
