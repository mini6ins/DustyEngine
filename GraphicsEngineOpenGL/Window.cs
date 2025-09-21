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
using ImGui_OpenTK.Backends;

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

    // Тестовый viewport данные
    private int testShaderProgram;
    private int testVAO, testVBO;
    private int uniformColor, uniformMVP;

    // Framebuffer для основной сцены
    private int sceneFramebuffer;
    private int sceneColorTexture;
    private int sceneDepthTexture;
    private int sceneFramebufferWidth = 800;
    private int sceneFramebufferHeight = 600;

    // Framebuffer для тестовой сцены
    private int testFramebuffer;
    private int testColorTexture;
    private int testDepthTexture;
    private int testFramebufferWidth = 512;
    private int testFramebufferHeight = 512;

    // Настройки
    private Vec3 testBackgroundColor = new Vec3(0.0f, 0.125f, 0.188f);
    private float testRotation = 0f;
    private bool initialized = false;

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
            AddRenderer(meshRenderer);
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

        CursorState = CursorState.Normal;
        
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        
        _camera.AspectRatio = Size.X / (float)Size.Y; 
        _projection = _camera.GetProjectionMatrix();

        InitializeImGui();
        InitializeViewports();
    }

    private void InitializeImGui()
    {
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();

        ImguiImplOpenTK4.Init(this);
        ImguiImplOpenGL3.Init();
    }

    private void InitializeViewports()
    {
        if (initialized) return;
        
        SetupTestShaders();
        SetupTestGeometry();
        SetupFramebuffers();
        initialized = true;
    }

    private void SetupTestShaders()
    {
        string vs = @"
            #version 330 core
            layout (location = 0) in vec3 aPos;
            uniform mat4 uMVP;
            void main()
            {
                gl_Position = uMVP * vec4(aPos, 1.0);
            }";
        string fs = @"
            #version 330 core
            out vec4 FragColor;
            uniform vec3 uColor;
            void main()
            {
                FragColor = vec4(uColor, 1.0);
            }";

        int v = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(v, vs);
        GL.CompileShader(v);

        int f = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(f, fs);
        GL.CompileShader(f);

        testShaderProgram = GL.CreateProgram();
        GL.AttachShader(testShaderProgram, v);
        GL.AttachShader(testShaderProgram, f);
        GL.LinkProgram(testShaderProgram);

        GL.DeleteShader(v);
        GL.DeleteShader(f);

        uniformColor = GL.GetUniformLocation(testShaderProgram, "uColor");
        uniformMVP = GL.GetUniformLocation(testShaderProgram, "uMVP");
    }

    private void SetupTestGeometry()
    {
        float[] vertices =
        {
             0.0f,  0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
        };

        testVAO = GL.GenVertexArray();
        testVBO = GL.GenBuffer();

        GL.BindVertexArray(testVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, testVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsage.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    private void SetupFramebuffers()
    {
        // Создаем framebuffer для основной сцены
        SetupFramebuffer(ref sceneFramebuffer, ref sceneColorTexture, ref sceneDepthTexture, sceneFramebufferWidth, sceneFramebufferHeight);
        
        // Создаем framebuffer для тестовой сцены
        SetupFramebuffer(ref testFramebuffer, ref testColorTexture, ref testDepthTexture, testFramebufferWidth, testFramebufferHeight);
    }

    private void SetupFramebuffer(ref int framebuffer, ref int colorTexture, ref int depthTexture, int width, int height)
    {
        framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

        colorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, colorTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8, 
            width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2d, colorTexture, 0);

        depthTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, depthTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24,
            width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2d, depthTexture, 0);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
        {
            throw new Exception("Framebuffer не готов!");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void ResizeFramebufferIfNeeded(ref int framebufferWidth, ref int framebufferHeight, 
        int colorTexture, int depthTexture, int width, int height)
    {
        // Добавляем небольшой порог, чтобы избежать постоянных изменений размера
        int newWidth = (int)Math.Max(64, width); // Минимум 64 пикселя
        int newHeight = (int)Math.Max(64, height);
        
        // Изменяем размер только если разница существенна (больше 10%)
        float widthDiff = Math.Abs(newWidth - framebufferWidth) / (float)framebufferWidth;
        float heightDiff = Math.Abs(newHeight - framebufferHeight) / (float)framebufferHeight;
        
        if (widthDiff > 0.1f || heightDiff > 0.1f)
        {
            framebufferWidth = newWidth;
            framebufferHeight = newHeight;

            GL.BindTexture(TextureTarget.Texture2d, colorTexture);
            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8,
                newWidth, newHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.BindTexture(TextureTarget.Texture2d, depthTexture);
            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24,
                newWidth, newHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                
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

        // Обновление анимации тестового треугольника
        testRotation += 0.02f;
        if (testRotation > 6.28f) testRotation = 0f;

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

        // Рендерим в framebuffer
        RenderMainSceneToFramebuffer();

        // Рендерим ImGui
        RenderImGui();

        SwapBuffers();
    }

    private void RenderMainSceneToFramebuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, sceneFramebuffer);
        GL.Viewport(0, 0, sceneFramebufferWidth, sceneFramebufferHeight);
        
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

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

    private void RenderTestSceneToFramebuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, testFramebuffer);
        GL.Viewport(0, 0, testFramebufferWidth, testFramebufferHeight);
        
        GL.ClearColor(testBackgroundColor.X, testBackgroundColor.Y, testBackgroundColor.Z, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var proj = Mat4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 
            (float)testFramebufferWidth / testFramebufferHeight, 0.1f, 10f);
        var view = Mat4.LookAt(new Vec3(0, 0, 2), Vec3.Zero, Vec3.UnitY);
        var model = Mat4.CreateRotationZ(testRotation);
        var mvp = model * view * proj;

        GL.UseProgram(testShaderProgram);

        unsafe
        {
            float[] arr = {
                mvp.M11, mvp.M12, mvp.M13, mvp.M14,
                mvp.M21, mvp.M22, mvp.M23, mvp.M24,
                mvp.M31, mvp.M32, mvp.M33, mvp.M34,
                mvp.M41, mvp.M42, mvp.M43, mvp.M44
            };
            fixed (float* ptr = arr)
            {
                GL.UniformMatrix4fv(uniformMVP, 1, false, ptr);
            }
        }

        GL.Uniform3f(uniformColor, 1f, 0f, 0f); // Красный треугольник

        GL.BindVertexArray(testVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
    }

    private void RenderImGui()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();

        ImGui.DockSpaceOverViewport();

        // Settings Panel - маленькая панель сверху
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Settings Panel"))
        {
            ImGui.Text($"Scene Objects: {_sceneObjects.Count}");
            ImGui.Text($"FPS: {_fps}");
        }
        ImGui.End();

        // Main Scene Viewport Panel - теперь свободно перемещаемое окно
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 170), ImGuiCond.FirstUseEver);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        ImGui.Begin("Main Scene Viewport", ImGuiWindowFlags.NoCollapse);
        {
            var availableSize = ImGui.GetContentRegionAvail();
            if (availableSize.X > 32 && availableSize.Y > 32) // Минимальный размер для стабильности
            {
                // Определяем желаемое соотношение сторон (16:9 или другое)
                float targetAspectRatio = 16.0f / 9.0f;
                
                // Вычисляем размер изображения, сохраняя пропорции
                float availableAspectRatio = availableSize.X / availableSize.Y;
                System.Numerics.Vector2 imageSize;
                
                if (availableAspectRatio > targetAspectRatio)
                {
                    // Окно шире чем нужно, ограничиваем по высоте
                    imageSize.Y = availableSize.Y;
                    imageSize.X = imageSize.Y * targetAspectRatio;
                }
                else
                {
                    // Окно выше чем нужно, ограничиваем по ширине
                    imageSize.X = availableSize.X;
                    imageSize.Y = imageSize.X / targetAspectRatio;
                }
                
                // Обновляем размер framebuffer'а только если он сильно отличается
                int targetWidth = (int)imageSize.X;
                int targetHeight = (int)imageSize.Y;
                
                ResizeFramebufferIfNeeded(ref sceneFramebufferWidth, ref sceneFramebufferHeight,
                    sceneColorTexture, sceneDepthTexture, targetWidth, targetHeight);
                
                // Обновляем проекционную матрицу для нового соотношения сторон
                _camera.AspectRatio = (float)sceneFramebufferWidth / sceneFramebufferHeight;
                _projection = _camera.GetProjectionMatrix();
                
                // Центрируем изображение
                var cursor = ImGui.GetCursorPos();
                cursor.X += (availableSize.X - imageSize.X) * 0.5f;
                cursor.Y += (availableSize.Y - imageSize.Y) * 0.5f;
                ImGui.SetCursorPos(cursor);
                
                // Отображаем изображение с правильным размером
                ImGui.Image(new IntPtr(sceneColorTexture), imageSize, 
                    new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
            }
        }
        ImGui.End();
        ImGui.PopStyleVar(2);

        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
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
        
        // Очистка основной сцены
        foreach (var vao in _vaoList)
            vao.Dispose();

        _shaderProgram.DeleteProgram();
        
        // Очистка тестовых ресурсов
        GL.DeleteVertexArray(testVAO);
        GL.DeleteBuffer(testVBO);
        GL.DeleteProgram(testShaderProgram);
        
        // Очистка framebuffer'ов
        GL.DeleteFramebuffer(sceneFramebuffer);
        GL.DeleteTexture(sceneColorTexture);
        GL.DeleteTexture(sceneDepthTexture);
        
        GL.DeleteFramebuffer(testFramebuffer);
        GL.DeleteTexture(testColorTexture);
        GL.DeleteTexture(testDepthTexture);
        
        // Очистка ImGui
        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        
        initialized = false;
        
        base.OnUnload();
    }
}