using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class RenderWindow : GameWindow
{
    private int _displayTexture;
    private int _shaderProgram;
    private int _vao;
    private int _vbo;
    
    private readonly FrameReceiver _frameReceiver;
    
    // Буферизация для плавности
    private FrameData? _currentFrame;
    private FrameData? _nextFrame;
    private readonly object _frameLock = new object();
    
    // Информация о текущей текстуре
    private int _currentTextureWidth = 0;
    private int _currentTextureHeight = 0;
    
    private const string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec2 aTexCoord;
        out vec2 texCoord;
        void main()
        {
            gl_Position = vec4(aPosition, 1.0);
            texCoord = aTexCoord;
        }";
    
    private const string FragmentShaderSource = @"
        #version 330 core
        in vec2 texCoord;
        out vec4 FragColor;
        uniform sampler2D frameTexture;
        void main()
        {
            FragColor = texture(frameTexture, texCoord);
        }";
    
    public RenderWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, FrameReceiver frameReceiver)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _frameReceiver = frameReceiver ?? throw new ArgumentNullException(nameof(frameReceiver));
        
        // Подписываемся на события получения кадров
        _frameReceiver.OnFrameReceived += OnFrameReceived;
    }
    
    private void OnFrameReceived(FrameData frameData)
    {
        lock (_frameLock)
        {
            _nextFrame = frameData;
        }
    }
    
    protected override void OnLoad()
    {
        base.OnLoad();
        
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f); // Темно-серый фон
        GL.Disable(EnableCap.DepthTest); // Не нужен для полноэкранного quad
        
        SetupShaders();
        SetupGeometry();
        SetupTexture();
        
        Console.WriteLine("RenderWindow загружен, ожидаю подключения к серверу...");
    }
    
    private void SetupShaders()
    {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, VertexShaderSource);
        GL.CompileShader(vertexShader);
        
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, FragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);
        
        
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }
    
    private void SetupGeometry()
    {
        // Полноэкранный quad
        float[] vertices = {
            -1.0f, -1.0f, 0.0f,  0.0f, 1.0f, // Нижний левый
             1.0f, -1.0f, 0.0f,  1.0f, 1.0f, // Нижний правый
             1.0f,  1.0f, 0.0f,  1.0f, 0.0f, // Верхний правый
            -1.0f,  1.0f, 0.0f,  0.0f, 0.0f  // Верхний левый
        };
        
        uint[] indices = { 0, 1, 2, 2, 3, 0 };
        
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
    }
    
    private void SetupTexture()
    {
        _displayTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _displayTexture);
        
        // Создаем черную текстуру по умолчанию
        byte[] blackPixels = new byte[4] { 32, 32, 32, 255 }; // Темно-серый
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, 
                     1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, blackPixels);
        
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        _currentTextureWidth = 1;
        _currentTextureHeight = 1;
    }
    
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        
        // Проверяем есть ли новый кадр для обновления
        FrameData? frameToUpdate = null;
        lock (_frameLock)
        {
            if (_nextFrame != null)
            {
                frameToUpdate = _nextFrame;
                _currentFrame = _nextFrame;
                _nextFrame = null;
            }
        }
        
        if (frameToUpdate != null)
        {
            UpdateTexture(frameToUpdate);
        }
    }
    
    private void UpdateTexture(FrameData frameData)
    {
        try
        {
            GL.BindTexture(TextureTarget.Texture2d, _displayTexture);
            
            // Обновляем размер текстуры только если изменился
            if (_currentTextureWidth != frameData.Width || _currentTextureHeight != frameData.Height)
            {
                Console.WriteLine($"Изменение размера текстуры: {_currentTextureWidth}x{_currentTextureHeight} -> {frameData.Width}x{frameData.Height}");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обновления текстуры: {ex.Message}");
        }
    }
    
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        GL.UseProgram(_shaderProgram);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, _displayTexture);
        GL.Uniform1f(GL.GetUniformLocation(_shaderProgram, "frameTexture"), 0);
        
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        
        SwapBuffers();
    }
    
    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        if (e.Key == Keys.Escape)
        {
            Close();
        }
        
        if (e.Key == Keys.F11)
        {
            WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
        }
    }
    
    protected override void OnUnload()
    {
        // Отписываемся от событий
        _frameReceiver.OnFrameReceived -= OnFrameReceived;
        
        // Освобождаем OpenGL ресурсы
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteTexture(_displayTexture);
        GL.DeleteProgram(_shaderProgram);
        
        base.OnUnload();
    }
}