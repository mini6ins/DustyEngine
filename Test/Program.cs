using System.Numerics;
using ImGui_OpenTK.Backends;
using ImGuiNET;
using OpenTK;
using OpenTK.Core.Utility;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace DustyEngineEditor;

/// <summary>
/// OpenTK 5.0: Один процесс, Engine рендерит в FBO, Editor показывает в ImGui
/// </summary>
public class EditorWithEmbeddedRenderer : IDisposable
{
    // ===== OpenTK 5.0 компоненты =====
    private WindowHandle? _window;
    private OpenGLContextHandle? _glContext;
    private bool _running = true;

    // ===== FBO для offscreen рендеринга Engine =====
    private int _engineFBO;
    private int _engineColorTexture;
    private int _engineDepthBuffer;
    private int _engineWidth = 800;
    private int _engineHeight = 600;

    // ===== Тестовый вращающийся треугольник =====
    private float _rotation = 0f;
    private float _rotationSpeed = 90f;
    private DateTime _lastFrame = DateTime.Now;

    public void Run()
    {
        Initialize();
        MainLoop();
    }

    private void Initialize()
    {
        // ===== ИНИЦИАЛИЗАЦИЯ TOOLKIT =====
        Toolkit.Init(new ToolkitOptions
        {
            ApplicationName = "DustyEngine Editor",
            Logger = new ConsoleLogger()
        });

        // ===== СОЗДАЕМ ОКНО (OpenTK 5.0 API) =====
        _window = Toolkit.Window.Create(new OpenGLGraphicsApiHints
        {
            Version = new Version(3, 3),
            Profile = OpenGLProfile.Core,
            DebugFlag = false
        });

        Toolkit.Window.SetTitle(_window, "DustyEngine Editor - OpenTK 5.0");

        Toolkit.Window.SetClientSize(_window, (1600, 900));

        Toolkit.Window.SetMode(_window, WindowMode.Normal);

        // ===== СОЗДАЕМ OPENGL КОНТЕКСТ =====
        _glContext = Toolkit.OpenGL.CreateFromWindow(_window);
        Toolkit.OpenGL.SetCurrentContext(_glContext);
        Toolkit.OpenGL.SetSwapInterval(1); // VSync

        // Загружаем OpenGL функции
        GLLoader.LoadBindings(new OpenTKBindingsContext(_glContext));

        Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
        Console.WriteLine($"GLSL Version: {GL.GetString(StringName.ShadingLanguageVersion)}");

        // ===== ИНИЦИАЛИЗАЦИЯ IMGUI =====
        Console.WriteLine("Creating ImGui context...");
        ImGui.CreateContext();

        Console.WriteLine("Setting up ImGui IO...");
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        // Отключаем ViewportsEnable для начала - это может вызывать проблемы
        // io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        ImGui.StyleColorsDark();

        Console.WriteLine("Initializing ImGui backends...");
        // Инициализируем backend'ы для OpenTK 5.0
        ImguiImplOpenTKPAL2.Init(_window, _glContext);
        Console.WriteLine("ImguiImplOpenTKPAL2 initialized");

        ImguiImplOpenGL3.Init();
        Console.WriteLine("ImguiImplOpenGL3 initialized");

        // ===== СОЗДАЕМ FBO =====
        Console.WriteLine("Creating FBO...");
        CreateEngineFBO();

        // ===== ПОДПИСЫВАЕМСЯ НА СОБЫТИЯ =====
        Console.WriteLine("Subscribing to events...");
        EventQueue.EventRaised += OnPlatformEvent;

        Console.WriteLine("✅ Editor initialized!");
    }

    private void CreateEngineFBO()
    {
        // 1. Текстура для цвета
        _engineColorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _engineColorTexture);
        GL.TexImage2D(
            TextureTarget.Texture2d,
            0,
            InternalFormat.Rgba,
            _engineWidth,
            _engineHeight,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            0
        );
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        // 2. Depth buffer
        _engineDepthBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _engineDepthBuffer);
        GL.RenderbufferStorage(
            RenderbufferTarget.Renderbuffer,
            InternalFormat.DepthComponent24,
            _engineWidth,
            _engineHeight
        );

        // 3. FBO
        _engineFBO = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _engineFBO);

        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2d,
            _engineColorTexture,
            0
        );

        GL.FramebufferRenderbuffer(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer,
            _engineDepthBuffer
        );

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.FramebufferComplete)
        {
            Console.WriteLine($"❌ Framebuffer error: {status}");
        }
        else
        {
            Console.WriteLine("✅ Framebuffer created!");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void MainLoop()
    {
        Console.WriteLine("Entering main loop...");

        while (_running)
        {
            // Обрабатываем события
            Toolkit.Window.ProcessEvents(false);

            // Проверяем не закрыто ли окно
            if (_window != null && Toolkit.Window.GetMode(_window) == WindowMode.Hidden)
            {
                _running = false;
                break;
            }

            // Вычисляем deltaTime
            var now = DateTime.Now;
            var deltaTime = (float)(now - _lastFrame).TotalSeconds;
            _lastFrame = now;

            // Update
            Update(deltaTime);

            // Render
            Render();

            // Swap buffers
            if (_glContext != null)
            {
                Toolkit.OpenGL.SwapBuffers(_glContext);
            }
        }

        Console.WriteLine("Exiting main loop...");
    }

    private void Update(float deltaTime)
    {
        // Обновляем вращение
        _rotation += _rotationSpeed * deltaTime;
        if (_rotation >= 360f) _rotation -= 360f;
    }

    private void Render()
    {
        // ===== ШАГ 1: РЕНДЕРИМ ENGINE В FBO =====
        RenderEngineToFBO();

        // ===== ШАГ 2: РЕНДЕРИМ EDITOR UI =====
        RenderEditorUI();
    }

    private void RenderEngineToFBO()
    {
        // Переключаемся на FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _engineFBO);
        GL.Viewport(0, 0, _engineWidth, _engineHeight);

        // Очищаем
        GL.ClearColor(0.2f, 0.3f, 0.4f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Enable(EnableCap.DepthTest);

        // ===== РИСУЕМ ТРЕУГОЛЬНИК ЧЕРЕЗ ШЕЙДЕРЫ (OpenTK 5.0 не поддерживает legacy GL) =====
        // Для примера используем простой immediate mode эмуляцию
        RenderTriangle();

        // Возвращаем дефолтный framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    // Простой треугольник без legacy OpenGL
    private void RenderTriangle()
    {
        // В реальном проекте используй VAO/VBO + шейдеры
        // Для примера просто очищаем с разным цветом каждый кадр
        float r = (MathF.Sin(_rotation * MathF.PI / 180f) + 1f) / 2f;
        float g = (MathF.Cos(_rotation * MathF.PI / 180f) + 1f) / 2f;
        float b = (MathF.Sin(_rotation * MathF.PI / 180f * 0.5f) + 1f) / 2f;

        GL.ClearColor(r * 0.5f, g * 0.5f, b * 0.5f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    private void RenderEditorUI()
    {
        // Получаем размер окна
        if (_window != null)
        {
            Toolkit.Window.GetFramebufferSize(_window, out var fbSize);

            // Очищаем экран
            GL.Viewport(0, 0, fbSize.X, fbSize.Y);
            GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        // ===== IMGUI FRAME START =====
        try
        {
            ImguiImplOpenTKPAL2.NewFrame();
            ImguiImplOpenGL3.NewFrame();
            ImGui.NewFrame();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ImGui NewFrame: {ex.Message}");
            return;
        }

        // Docking - используем ID вместо указателя
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport().ID);

        // ===== ОКНО С КОНТРОЛАМИ =====
        ImGui.Begin("Controls");
        ImGui.Text($"FPS: {1.0f / ImGui.GetIO().DeltaTime:F1}");
        ImGui.SliderFloat("Rotation Speed", ref _rotationSpeed, 0f, 360f);

        if (ImGui.Button("Reset Rotation"))
        {
            _rotation = 0f;
        }

        if (ImGui.Button("Close"))
        {
            _running = false;
        }

        ImGui.End();

        // ===== VIEWPORT С ENGINE ТЕКСТУРОЙ =====
        ImGui.Begin("Engine Viewport");

        var viewportSize = ImGui.GetContentRegionAvail();

        // ✅ ПОКАЗЫВАЕМ ТЕКСТУРУ ИЗ FBO!
        ImGui.Image(
            (IntPtr)_engineColorTexture,
            new Vector2(_engineWidth, _engineHeight),
            new Vector2(0, 1), // UV перевернуты для OpenGL
            new Vector2(1, 0)
        );

        ImGui.Text($"Viewport: {_engineWidth}x{_engineHeight}");
        ImGui.Text("(В реальном проекте используй VAO/VBO + шейдеры)");

        ImGui.End();

        // ===== SCENE HIERARCHY =====
        ImGui.Begin("Scene Hierarchy");
        ImGui.Text("- Root");
        if (ImGui.TreeNode("GameObject"))
        {
            ImGui.Text("  - Transform");
            ImGui.Text("  - MeshRenderer");
            ImGui.TreePop();
        }
        ImGui.End();

        // ===== IMGUI RENDER =====
        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

        // Multi-viewport (закомментировано пока не заработает базовая версия)
        /*
        var io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }
        */
    }

    private void OnPlatformEvent(PalHandle? handle, PlatformEventType type, EventArgs args)
    {
        // События обрабатываются backend'ом ImguiImplOpenTKPAL2
        if (args is CloseEventArgs closeEvent)
        {
            if (closeEvent.Window == _window)
            {
                _running = false;
            }
        }
    }

    public void Dispose()
    {
        Console.WriteLine("Disposing...");

        // Cleanup
        EventQueue.EventRaised -= OnPlatformEvent;

        Console.WriteLine("Cleaning up GL resources...");
        try
        {
            GL.DeleteFramebuffer(_engineFBO);
            GL.DeleteTexture(_engineColorTexture);
            GL.DeleteRenderbuffer(_engineDepthBuffer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up GL resources: {ex.Message}");
        }

        Console.WriteLine("Shutting down ImGui...");
        try
        {
            ImguiImplOpenGL3.Shutdown();
            ImguiImplOpenTKPAL2.Shutdown();
            ImGui.DestroyContext();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error shutting down ImGui: {ex.Message}");
        }

        Console.WriteLine("Destroying OpenGL context...");
        if (_glContext != null)
        {
            try
            {
                Toolkit.OpenGL.DestroyContext(_glContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error destroying GL context: {ex.Message}");
            }
        }

        Console.WriteLine("Destroying window...");
        if (_window != null)
        {
            try
            {
                Toolkit.Window.Destroy(_window);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error destroying window: {ex.Message}");
            }
        }

        Console.WriteLine("Dispose complete");
    }
}

// ===== ТОЧКА ВХОДА =====
class Program
{
    static void Main()
    {
        try
        {
            Console.WriteLine("Starting application...");
            using var app = new EditorWithEmbeddedRenderer();
            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex}");
        }
    }
}

// ===== BINDINGS CONTEXT ДЛЯ OPENTK 5.0 =====
class OpenTKBindingsContext : IBindingsContext
{
    private readonly OpenGLContextHandle _context;

    public OpenTKBindingsContext(OpenGLContextHandle context)
    {
        _context = context;
    }

    public IntPtr GetProcAddress(string procName)
    {
        return Toolkit.OpenGL.GetProcedureAddress(_context, procName);
    }
}
