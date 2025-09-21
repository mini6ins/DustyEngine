// Program.cs
using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Graphics.OpenGL4;

namespace DustyEngineGUI;

public class OpenGLViewport
{
    private int _framebuffer;
    private int _colorTexture;
    private int _depthTexture;
    private int _width = 800;
    private int _height = 600;

    public int Width => _width;
    public int Height => _height;
    public int TextureId => _colorTexture;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        CreateFramebuffer();
    }

    private void CreateFramebuffer()
    {
        // Создаем framebuffer
        _framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

        // Создаем цветную текстуру
        _colorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.f, _colorTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _width, _height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // Создаем depth текстуру
        _depthTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depthTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // Прикрепляем к framebuffer
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTexture, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthTexture, 0);

        // Проверяем статус
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Framebuffer не создался!");

        // Возвращаемся к дефолтному framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Resize(int newWidth, int newHeight)
    {
        if (_width == newWidth && _height == newHeight) return;

        _width = newWidth;
        _height = newHeight;

        // Пересоздаем текстуры
        GL.BindTexture(TextureTarget.Texture2d, _colorTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, PixelInternalFormat.Rgb, _width, _height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);

        GL.BindTexture(TextureTarget.Texture2d, _depthTexture);
        GL.TexImage2D(TextureTarget.Texture2d, 0, PixelInternalFormat.DepthComponent24, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        GL.BindTexture(TextureTarget.Texture2d, 0);
    }

    public void BeginRender()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        GL.Viewport(0, 0, _width, _height);
    }

    public void EndRender()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void RenderScene()
    {
        // Очищаем буферы
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Здесь можете рисовать что угодно
        // Пока что просто градиент
        GL.Begin(PrimitiveType.Triangles);
        
        GL.Color3f(1.0f, 0.0f, 0.0f); // Красный
        GL.Vertex2f(-0.6f, -0.4f);
        
        GL.Color3f(0.0f, 1.0f, 0.0f); // Зеленый  
        GL.Vertex2f(0.6f, -0.4f);
        
        GL.Color3f(0.0f, 0.0f, 1.0f); // Синий
        GL.Vertex2f(0.0f, 0.6f);
        
        GL.End();
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(_framebuffer);
        GL.DeleteTexture(_colorTexture);
        GL.DeleteTexture(_depthTexture);
    }
}

public sealed class MainWindow : GameWindow
{
    private ImGuiController? _imgui;
    private OpenGLViewport? _viewport;

    public MainWindow()
        : base(GameWindowSettings.Default, new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720),
            Title = "OpenTK в ImGui",
            Flags = ContextFlags.ForwardCompatible
        })
    { }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        GL.Enable(EnableCap.DepthTest);
        
        _imgui = new ImGuiController(ClientSize.X, ClientSize.Y);
        _viewport = new OpenGLViewport();
        _viewport.Initialize(800, 600);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // Рендерим OpenGL сцену в текстуру
        _viewport?.BeginRender();
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        _viewport?.RenderScene();
        _viewport?.EndRender();

        // Рендерим ImGui
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _imgui?.Update(this, (float)args.Time);

        // ---------- ImGui UI ----------
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        
        ImGuiWindowFlags hostFlags = ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar |
                                     ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
                                     ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus |
                                     ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground;
        
        ImGui.Begin("DockSpaceHost", hostFlags);
        ImGui.PopStyleVar(2);

        var dockspaceId = ImGui.GetID("MyDockSpace");
        ImGui.DockSpace(dockspaceId, System.Numerics.Vector2.Zero, ImGuiDockNodeFlags.None);
        ImGui.End();

        // Окно с OpenGL viewport
        ImGui.Begin("OpenGL Viewport");
        
        var contentRegion = ImGui.GetContentRegionAvail();
        int newWidth = (int)contentRegion.X;
        int newHeight = (int)contentRegion.Y;
        
        if (newWidth > 0 && newHeight > 0)
        {
            _viewport?.Resize(newWidth, newHeight);
            
            // Отображаем текстуру
            ImGui.Image((IntPtr)(_viewport?.TextureId ?? 0), contentRegion, 
                       System.Numerics.Vector2.Zero, System.Numerics.Vector2.One, 
                       System.Numerics.Vector4.One, System.Numerics.Vector4.Zero);
        }
        
        ImGui.End();

        // Другие окна
        ImGui.Begin("Панель инструментов");
        ImGui.Text("Здесь могут быть настройки");
        ImGui.Button("Кнопка 1");
        ImGui.Button("Кнопка 2");
        ImGui.End();

        ImGui.ShowDemoWindow();

        _imgui?.Render();
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _imgui?.WindowResized(ClientSize.X, ClientSize.Y);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        _viewport?.Dispose();
        _imgui?.Dispose();
    }
}

public static class Program
{
    public static void Main()
    {
        using var window = new MainWindow();
        window.Run();
    }
}