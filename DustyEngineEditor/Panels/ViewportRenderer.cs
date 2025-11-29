using ImGui_OpenTK.Backends;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class ViewportRenderer : GameWindow
{
    private IRemoteRenderer _remoteRenderer;
    private int _texture;
    private float _time = 0f;
    private FrameData?[] _frameBuffer = new FrameData?[3];
    private volatile int _readyBufferIndex = 0;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private Task? _fetcherTask;
    
    private int _framesReceived = 0;
    private int _framesDisplayed = 0;
    private DateTime _lastStatsTime = DateTime.Now;
    private float _lastReceivedTimestamp = -1f;
    private int _currentTextureWidth = 0;
    private int _currentTextureHeight = 0;

    private RendererUI _ui;

    public ViewportRenderer(IRemoteRenderer remoteRenderer)
        : base(GameWindowSettings.Default,
            new NativeWindowSettings()
            {
                Size = new Vector2i(1024, 768),
                Title = "Remote Renderer Client",
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
        
        _ui = new RendererUI(_remoteRenderer, this);
        
        _fetcherTask = Task.Run(FetchFramesLoop);
        Console.WriteLine("Client ready. WASD to move camera, mouse to rotate.");
    }

    private void InitializeTexture()
    {
        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _texture);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        byte[] px = new byte[] { 32, 32, 32, 255 };
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
                    int writeIndex = 1 - _readyBufferIndex;
                    _frameBuffer[writeIndex] = frame;
                    _readyBufferIndex = writeIndex;
                    _lastReceivedTimestamp = frame.Timestamp;
                    _framesReceived++;
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

        UpdateStats();

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        _ui.Update(KeyboardState);
    }

    private void UpdateStats()
    {
        var now = DateTime.Now;
        if ((now - _lastStatsTime).TotalSeconds >= 1.0)
        {
            string helpText = _ui.ShowHelp ? " | H:hide" : " | H:help";
            Title = $"Remote Renderer - Recv: {_framesReceived} | Display: {_framesDisplayed} FPS{helpText}";
            _framesReceived = 0;
            _framesDisplayed = 0;
            _lastStatsTime = now;
        }
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
        
        _ui.Render(_texture, _currentTextureWidth, _currentTextureHeight, 
                   ref _framesReceived, ref _framesDisplayed);
        
        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
        
        SwapBuffers();
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

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }
}