using DustyEngine;
using DustyEngineEditor.Panels.ViewPortPanel.RemoteRenderer;
using GraphicsEngineOpenGL;
using ImGui_OpenTK.Backends;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DustyEngineEditor.Panels;

public class ViewportRenderer : GameWindow
{
    private readonly IRemoteRenderer _remoteRenderer;
    private int _texture;
    private float _time;
    private readonly FrameData?[] _frameBuffer = new FrameData?[3];
    private volatile int _readyBufferIndex;
    private readonly CancellationTokenSource _cts = new();
    private Task? _fetcherTask;

    private int _framesDisplayed;
    private int _currentTextureWidth;
    private int _currentTextureHeight;

    private RendererUI _ui = null!;

    public ViewportRenderer(IRemoteRenderer remoteRenderer)
        : base(GameWindowSettings.Default,
            new NativeWindowSettings()
            {
                ClientSize = new Vector2i(1024, 768),
                Title = "DustyEngineEditor " + Editor.EditorVersion + "v",
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                Flags = ContextFlags.Default
            })
    {
        _remoteRenderer = remoteRenderer;
        UpdateFrequency = 200;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();

        ImguiImplOpenTK4.Init(this);
        ImguiImplOpenGL3.Init();

        InitializeTexture();

        _ui = new RendererUI(_remoteRenderer);

        _fetcherTask = Task.Run(FetchFramesLoop);
        Debug.Log("Editor ready. WASD to move camera, mouse to rotate.");
    }

    private void InitializeTexture()
    {
        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _texture);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        byte[] px = [32, 32, 32, 255];
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, 1, 1, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, px);

        _currentTextureWidth = 1;
        _currentTextureHeight = 1;
    }

    private async Task FetchFramesLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var frame = await _remoteRenderer.GetFrameData(_time);

                if (frame?.PixelData?.Length > 0)
                {
                    var writeIndex = 1 - _readyBufferIndex;
                    _frameBuffer[writeIndex] = frame;
                    _readyBufferIndex = writeIndex;
                }

                await Task.Delay(1, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching frame: {ex.Message}");
                await Task.Delay(16, _cts.Token);
            }
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        _time += (float)args.Time;

        var frameToUpdate = _frameBuffer[_readyBufferIndex];
        if (frameToUpdate?.PixelData?.Length > 0)
        {
            UpdateTexture(frameToUpdate);
        }

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        _ui.Update(KeyboardState);
    }


    private void UpdateTexture(FrameData frameData)
    {
        try
        {
            GL.BindTexture(TextureTarget.Texture2d, _texture);

            if (_currentTextureWidth != frameData.Width || _currentTextureHeight != frameData.Height)
            {
                _currentTextureWidth = frameData.Width;
                _currentTextureHeight = frameData.Height;
            }

            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba,
                frameData.Width, frameData.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, frameData.PixelData);

            GL.BindTexture(TextureTarget.Texture2d, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating texture: {ex.Message}");
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();

        _ui.Render(_texture, _currentTextureWidth, _currentTextureHeight, ref _framesDisplayed);

        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        _cts.Cancel();
        _fetcherTask?.Wait(TimeSpan.FromSeconds(2));
        _cts.Dispose();

        GL.DeleteTexture(_texture);

        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        ImGui.DestroyContext();
    }
}
