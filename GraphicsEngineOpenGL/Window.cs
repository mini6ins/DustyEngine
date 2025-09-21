using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using System.Runtime.InteropServices;
using DustyEngine;
using DustyEngine.Components;
using GraphicsEngineOpenGL.RenderUtils;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Utils;

// Используем алиасы для OpenTK типов
using Vec3 = OpenTK.Mathematics.Vector3;
using Mat4 = OpenTK.Mathematics.Matrix4;
using MathHelper = OpenTK.Mathematics.MathHelper;

namespace GraphicsEngineOpenGL;

public class RenderableObject
{
    public int VaoIndex;
    public Transform Transform = new();
    public MeshRenderer MeshRenderer; 
}

public class Window : GameWindow
{
    private float _frameTime;
    private int _fps;
    private readonly string _windowName;

    private Camera _camera;
    private Matrix4 _projection;
    private CursorState _cursorState;

    // Основная сцена
    private ShaderProgram _shaderProgram;
    private readonly List<VAOManager> _vaoList = new();
    private readonly List<RenderableObject> _sceneObjects = new();

    // Framebuffer для основной сцены
    private int sceneFramebuffer;
    private int sceneColorTexture;
    private int sceneDepthTexture;
    private int sceneFramebufferWidth = 800;
    private int sceneFramebufferHeight = 600;

    // ImGui Manager (опционально)
    private ImGuiManager _imguiManager;
    private bool _useImGui = false;

    private bool initialized = false;

    public Window(GameWindowSettings gws, NativeWindowSettings nws, List<MeshRenderer> allRenderers,
        string vertShaderPath, string fragShaderPath, string windowName,
        Camera camera, bool isVsync = true, CursorState cursorState = CursorState.Normal, bool useImGui = false)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        this._camera = camera;
        _cursorState = cursorState;
        _useImGui = useImGui;
        
        Debug.Log(GL.GetString(StringName.Version), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.Vendor), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.Renderer), Debug.LogLevel.Info, true);
        Debug.Log(GL.GetString(StringName.ShadingLanguageVersion), Debug.LogLevel.Info, true);

        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;

        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);
        
        foreach (var meshRenderer in allRenderers)
        {
            AddRenderer(meshRenderer);
        }

        if (_useImGui)
        {
            InitializeImGui();
        }
    }

    public int AddRenderer(MeshRenderer meshRenderer)
    {
        if (meshRenderer == null)
        {
            Debug.Log("Cannot add null MeshRenderer", Debug.LogLevel.Error, true);
            return -1;
        }

        var mesh = meshRenderer.GetMesh();
        if (mesh == null)
        {
            Debug.Log($"MeshRenderer has no valid mesh data, skipping renderer for {meshRenderer.Parent?.Name ?? "unknown object"}", Debug.LogLevel.Warning, true);
            return -1;
        }

        if (mesh.Vertices == null || mesh.Indices == null)
        {
            Debug.Log($"Mesh has null vertices or indices, skipping renderer for {meshRenderer.Parent?.Name ?? "unknown object"}", Debug.LogLevel.Warning, true);
            return -1;
        }

        var vao = new VAOManager(_shaderProgram);
        vao.CreateVAO(mesh.Vertices, mesh.Indices);
        _vaoList.Add(vao);

        var renderableObject = new RenderableObject
        {
            VaoIndex = _vaoList.Count - 1,
            Transform = meshRenderer.Parent.GetComponent<Transform>(),
            MeshRenderer = meshRenderer,
        };

        _sceneObjects.Add(renderableObject);
    
        Debug.Log($"Added new renderer. Total objects: {_sceneObjects.Count}", Debug.LogLevel.Info, true);
    
        return _sceneObjects.Count - 1;
    }
    
    public bool RemoveRenderer(int objectId)
    {
        if (objectId < 0 || objectId >= _sceneObjects.Count)
        {
            Debug.Log($"Invalid object ID: {objectId}", Debug.LogLevel.Warning, true);
            return false;
        }

        var obj = _sceneObjects[objectId];
 
        if (obj.VaoIndex < _vaoList.Count)
        {
            _vaoList[obj.VaoIndex].Dispose();
            _vaoList.RemoveAt(obj.VaoIndex);
            
            for (int i = 0; i < _sceneObjects.Count; i++)
            {
                if (_sceneObjects[i].VaoIndex > obj.VaoIndex)
                {
                    _sceneObjects[i].VaoIndex--;
                }
            }
        }

        _sceneObjects.RemoveAt(objectId);
        
        Debug.Log($"Removed renderer. Total objects: {_sceneObjects.Count}", Debug.LogLevel.Info, true);
        
        return true;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        Input.Update(KeyboardState);

        GL.ClearColor(173/255f, 216/255f, 230/255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        CursorState = _cursorState;
        
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        
        _camera.AspectRatio = Size.X / (float)Size.Y; 
        _projection = _camera.GetProjectionMatrix();

        if (_useImGui)
        {
            SetupFramebuffer();
        }
        
        initialized = true;
    }

    private void InitializeImGui()
    {
        _imguiManager = new ImGuiManager();
        
        // Настраиваем callbacks для передачи данных в ImGui
        _imguiManager.GetSceneObjectCount = () => _sceneObjects.Count;
        _imguiManager.GetFPS = () => _fps;
        _imguiManager.GetSceneTexture = () => sceneColorTexture;
        _imguiManager.GetSceneSize = () => (sceneFramebufferWidth, sceneFramebufferHeight);
        _imguiManager.OnSceneResize = (width, height) => ResizeFramebuffer(width, height);
        
        _imguiManager.Initialize(this);
    }

    private void SetupFramebuffer()
    {
        sceneFramebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, sceneFramebuffer);

        sceneColorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, sceneColorTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8, 
            sceneFramebufferWidth, sceneFramebufferHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2d, sceneColorTexture, 0);

        sceneDepthTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, sceneDepthTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24,
            sceneFramebufferWidth, sceneFramebufferHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2d, sceneDepthTexture, 0);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
        {
            throw new Exception("Framebuffer не готов!");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void ResizeFramebuffer(int width, int height)
    {
        int newWidth = (int)Math.Max(64, width); 
        int newHeight = (int)Math.Max(64, height);
        
        float widthDiff = Math.Abs(newWidth - sceneFramebufferWidth) / (float)sceneFramebufferWidth;
        float heightDiff = Math.Abs(newHeight - sceneFramebufferHeight) / (float)sceneFramebufferHeight;
        
        if (widthDiff > 0.1f || heightDiff > 0.1f)
        {
            sceneFramebufferWidth = newWidth;
            sceneFramebufferHeight = newHeight;

            GL.BindTexture(TextureTarget.Texture2d, sceneColorTexture);
            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8,
                newWidth, newHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.BindTexture(TextureTarget.Texture2d, sceneDepthTexture);
            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24,
                newWidth, newHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                
            _camera.AspectRatio = (float)newWidth / newHeight;
            _projection = _camera.GetProjectionMatrix();
                
            Debug.Log($"Resized framebuffer to {newWidth}x{newHeight}", Debug.LogLevel.Info, true);
        }
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
            Title = $"{_windowName} : FPS - {_fps} | Objects: {_sceneObjects.Count}";
            _frameTime = 0.0f;
            _fps = 0;
        }

        //Debug
        if (Input.IsKeyDown(KeyCode.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        if (Input.IsKeyDown(KeyCode.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        if (_useImGui)
        {
            RenderSceneToFramebuffer();
            RenderImGuiInterface();
        }
        else
        {
            RenderSceneDirect();
        }

        SwapBuffers();
    }

    private void RenderSceneToFramebuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, sceneFramebuffer);
        GL.Viewport(0, 0, sceneFramebufferWidth, sceneFramebufferHeight);
        
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderScene();
    }

    private void RenderSceneDirect()
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
        {
            if (obj.MeshRenderer.IsActiveAndEnabled)
            {
                RenderObject(obj);
            }
        }

        _shaderProgram.DeactiveProgram();
    }

    private void RenderImGuiInterface()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _imguiManager?.NewFrame();
        _imguiManager?.RenderUI();
        _imguiManager?.Render();
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
        if (!initialized) return;
        
        foreach (var vao in _vaoList)
            vao.Dispose();

        _shaderProgram.DeleteProgram();
        
        if (_useImGui)
        {
            GL.DeleteFramebuffer(sceneFramebuffer);
            GL.DeleteTexture(sceneColorTexture);
            GL.DeleteTexture(sceneDepthTexture);
            
            _imguiManager?.Shutdown();
        }
        
        initialized = false;
        
        base.OnUnload();
    }
}