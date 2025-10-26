using System.Runtime.InteropServices;
using GraphicsEngineOpenGL;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Utils;

public class RenderWindow : GameWindow
{
    private int _displayTexture;
    private int _shaderProgram;
    private int _vao;
    private int _vbo;

    private readonly FrameReceiver _frameReceiver;

    private MMFShared.FrameData? _nextFrame;
    private readonly Lock _frameLock = new();

    private int _currentTextureWidth;
    private int _currentTextureHeight;

    private ImGuiManager _imgui;

    private bool _capturingInput;
    private readonly HashSet<Keys> _pressedKeys = [];

    private float _lastMouseX;
    private float _lastMouseY;
    private bool _firstMouseMove = true;

    private const string VertexShaderSource = """
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec2 aTexCoord;
        out vec2 texCoord;
        void main()
        {
            gl_Position = vec4(aPosition, 1.0);
            texCoord = aTexCoord;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec2 texCoord;
        out vec4 FragColor;
        uniform sampler2D frameTexture;
        void main()
        {
            FragColor = texture(frameTexture, texCoord);
        }
        """;

    public RenderWindow(GameWindowSettings gws, NativeWindowSettings nws, FrameReceiver frameReceiver)
        : base(gws, nws)
    {
        _frameReceiver = frameReceiver ?? throw new ArgumentNullException(nameof(frameReceiver));
        _frameReceiver.OnFrameReceived += OnFrameReceived;
    }

    private void OnFrameReceived(MMFShared.FrameData frameData)
    {
        lock (_frameLock)
        {
            _nextFrame = frameData;
        }
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Disable(EnableCap.DepthTest);

        SetupShaders();
        SetupGeometry();
        SetupTexture();

        _imgui = new ImGuiManager();
        _imgui.Initialize(this);
    }

    private void SetupShaders()
    {
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, VertexShaderSource);
        GL.CompileShader(vs);

        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, FragmentShaderSource);
        GL.CompileShader(fs);

        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vs);
        GL.AttachShader(_shaderProgram, fs);
        GL.LinkProgram(_shaderProgram);

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        GL.UseProgram(_shaderProgram);
        int loc = GL.GetUniformLocation(_shaderProgram, "frameTexture");
        if (loc >= 0) GL.Uniform1i(loc, 0);
        GL.UseProgram(0);
    }

    private void SetupGeometry()
    {
        float[] vertices =
        [
            -1.0f, -1.0f, 0.0f, 0.0f, 1.0f,
            1.0f, -1.0f, 0.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 0.0f, 1.0f, 0.0f,
            -1.0f, 1.0f, 0.0f, 0.0f, 0.0f
        ];
        uint[] indices = [0, 1, 2, 2, 3, 0];

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        int ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsage.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsage.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    private void SetupTexture()
    {
        _displayTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _displayTexture);

        byte[] px = [32, 32, 32, 255];
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, 1, 1, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, px);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _currentTextureWidth = 1;
        _currentTextureHeight = 1;

        GL.BindTexture(TextureTarget.Texture2d, 0);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        MMFShared.FrameData? frameToUpdate = null;
        lock (_frameLock)
        {
            if (_nextFrame != null)
            {
                frameToUpdate = _nextFrame;
                _nextFrame = null;
            }
        }

        if (frameToUpdate != null)
            UpdateTexture(frameToUpdate);
    }

    private void UpdateTexture(MMFShared.FrameData frameData)
    {
        try
        {
            GL.BindTexture(TextureTarget.Texture2d, _displayTexture);

            if (_currentTextureWidth != frameData.Width || _currentTextureHeight != frameData.Height)
            {
                _currentTextureWidth = frameData.Width;
                _currentTextureHeight = frameData.Height;
            }

            var handle = GCHandle.Alloc(frameData.PixelData, GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba,
                    frameData.Width, frameData.Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte,
                    handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }

            GL.BindTexture(TextureTarget.Texture2d, 0);
        }
        catch
        {
            // ignored
        }
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        _imgui.NewFrame();

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.Begin("Control Panel");
        ImGui.Text($"Connected: {_frameReceiver.IsConnected}");
        ImGui.Text($"Texture: {_currentTextureWidth} x {_currentTextureHeight}");

        ImGui.Separator();
        ImGui.Checkbox("Capture Input (Ctrl+I)", ref _capturingInput);
        ImGui.Text(_capturingInput ? "Input: CAPTURING" : "Input: GUI mode");

        if (ImGui.Button("Fullscreen (F11)"))
            WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
        ImGui.End();

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.Begin("Frame Preview", ImGuiWindowFlags.NoCollapse);
        {
            var avail = ImGui.GetContentRegionAvail();
            if (avail.X >= 2 && avail.Y >= 2 && _currentTextureWidth > 0 && _currentTextureHeight > 0)
            {
                float textureAspect = (float)_currentTextureWidth / _currentTextureHeight;
                float availableAspect = avail.X / avail.Y;

                System.Numerics.Vector2 displaySize;
                if (textureAspect > availableAspect)
                {
                    displaySize.X = avail.X;
                    displaySize.Y = avail.X / textureAspect;
                }
                else
                {
                    displaySize.Y = avail.Y;
                    displaySize.X = avail.Y * textureAspect;
                }

                var cursor = ImGui.GetCursorPos();
                cursor.X += (avail.X - displaySize.X) * 0.5f;
                cursor.Y += (avail.Y - displaySize.Y) * 0.5f;
                ImGui.SetCursorPos(cursor);

                ImGui.Image(new IntPtr(_displayTexture), displaySize,
                    new System.Numerics.Vector2(0, 1),
                    new System.Numerics.Vector2(1, 0));
            }
            else
            {
                ImGui.Text($"Waiting for frames... ({_currentTextureWidth}x{_currentTextureHeight})");
            }
        }
        ImGui.End();

        _imgui.Render();
        SwapBuffers();
    }


    private void SendInputEvent(MMFShared.InputEventType type, int keyCode = 0, 
        float mouseX = 0, float mouseY = 0, int mouseButton = 0, float wheelDelta = 0)
    {
        _frameReceiver.SendInputEvent(new MMFShared.InputEvent
        {
            Type = (int)type,
            KeyCode = keyCode,
            MouseX = mouseX,
            MouseY = mouseY,
            MouseButton = mouseButton,
            WheelDelta = wheelDelta
        });
    }


    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Keys.I when e.Control:
            {
                _capturingInput = !_capturingInput;
                CursorState = _capturingInput ? CursorState.Grabbed : CursorState.Normal;

                if (!_capturingInput)
                {
                    _pressedKeys.Clear();
                    _firstMouseMove = true;
                }

                return;
            }
            case Keys.Escape:
            {
                if (_capturingInput)
                {
                    _capturingInput = false;
                    CursorState = CursorState.Normal;
                    _pressedKeys.Clear();
                    _firstMouseMove = true;
                }
                else
                {
                    Close();
                }

                return;
            }
            case Keys.F11:
                WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
                return;
        }

        if (_capturingInput && _pressedKeys.Add(e.Key))
        {
            SendInputEvent(MMFShared.InputEventType.KeyDown, keyCode: (int)e.Key);
        }
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (!_capturingInput) return;

        _pressedKeys.Remove(e.Key);
        SendInputEvent(MMFShared.InputEventType.KeyUp, keyCode: (int)e.Key);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_capturingInput) return;

        float normalizedX = e.X / Size.X;
        float normalizedY = e.Y / Size.Y;

        float deltaX = 0;
        float deltaY = 0;

        if (!_firstMouseMove)
        {
            deltaX = normalizedX - _lastMouseX;
            deltaY = normalizedY - _lastMouseY;
        }
        else
        {
            _firstMouseMove = false;
        }

        _lastMouseX = normalizedX;
        _lastMouseY = normalizedY;

        SendInputEvent(MMFShared.InputEventType.MouseMove, mouseX: deltaX, mouseY: deltaY);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (!_capturingInput) return;

        float normalizedX = MouseState.Position.X / Size.X;
        float normalizedY = MouseState.Position.Y / Size.Y;

        SendInputEvent(MMFShared.InputEventType.MouseDown, 
            mouseX: normalizedX, mouseY: normalizedY, mouseButton: (int)e.Button);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (!_capturingInput) return;

        float normalizedX = MouseState.Position.X / Size.X;
        float normalizedY = MouseState.Position.Y / Size.Y;

        SendInputEvent(MMFShared.InputEventType.MouseUp, 
            mouseX: normalizedX, mouseY: normalizedY, mouseButton: (int)e.Button);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!_capturingInput) return;

        SendInputEvent(MMFShared.InputEventType.MouseWheel, wheelDelta: e.OffsetY);
    }
    
    protected override void OnUnload()
    {
        _frameReceiver.OnFrameReceived -= OnFrameReceived;

        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteTexture(_displayTexture);
        GL.DeleteProgram(_shaderProgram);

        _imgui?.Shutdown();

        base.OnUnload();
    }
}