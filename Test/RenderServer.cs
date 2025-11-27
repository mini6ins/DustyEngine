using System.Diagnostics;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Buffer = System.Buffer;
using GameWindow = OpenTK.Windowing.Desktop.GameWindow;
using Vector2 = OpenTK.Vector2;
using Vector3 = OpenTK.Vector3;


namespace DustyEngine.Runner;

public class RenderServer : IDisposable
{
    private GameWindow? _window;
    private readonly int _frameBufferWidth = 800;
    private readonly int _frameBufferHeight = 600;
    private int _vao;
    private int _vbo;
    private int _shaderProgram;
    private bool _initialized = false;
    private int _frameCount = 0;
    private volatile bool _disposed = false;

    // Тройная буферизация для 200 FPS
    private readonly FrameData[] _frameBuffer = new FrameData[3];
    private int _writeIndex = 0;
    private readonly object _bufferLock = new object();

    private float _currentTime = 0f;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    // Input состояние
    private float _triangleScale = 1.0f;
    private float _rotationSpeed = 1.0f;
    private Vector3 _triangleColor = new Vector3(1.0f, 1.0f, 1.0f);
    private Vector2 _trianglePosition = Vector2.Zero;

    private const string VertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

out vec3 vertexColor;
uniform float uTime;
uniform float uScale;
uniform float uRotationSpeed;
uniform vec2 uPosition;

void main()
{
    vec3 scaledPos = aPosition * uScale;
    float angle = uTime * uRotationSpeed;
    float cosA = cos(angle);
    float sinA = sin(angle);
    vec2 rotated = vec2(
        scaledPos.x * cosA - scaledPos.y * sinA,
        scaledPos.x * sinA + scaledPos.y * cosA
    );
    gl_Position = vec4(rotated + uPosition, scaledPos.z, 1.0);
    vertexColor = aColor;
}
";

    private const string FragmentShaderSource = @"
#version 330 core
in vec3 vertexColor;
out vec4 FragColor;
uniform vec3 uColorTint;

void main()
{
    FragColor = vec4(vertexColor * uColorTint, 1.0);
}
";

    public RenderServer()
    {
        Console.WriteLine("RenderServer constructor called");

        // Инициализация буфера кадров
        int pixelCount = _frameBufferWidth * _frameBufferHeight * 4;
        for (int i = 0; i < _frameBuffer.Length; i++)
        {
            _frameBuffer[i] = new FrameData
            {
                Width = _frameBufferWidth,
                Height = _frameBufferHeight,
                PixelData = new byte[pixelCount]
            };
        }

        try
        {
            InitializeOpenGL();
            _initialized = true;
            Console.WriteLine("RenderServer initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize RenderServer: {ex.Message}");
            throw;
        }
    }

    public void RunRenderLoop()
    {
        if (!_initialized || _window == null)
        {
            Console.WriteLine("Cannot run render loop - not initialized");
            return;
        }

        Console.WriteLine("Starting render loop at 200 FPS...");
        const double targetFrameTime = 1.0 / 200.0; // 5ms per frame
        var frameTimer = Stopwatch.StartNew();
        double nextFrameTime = 0;

        while (!_disposed)
        {
            try
            {
                double currentTime = frameTimer.Elapsed.TotalSeconds;

                if (currentTime >= nextFrameTime)
                {
                    _currentTime = (float)_stopwatch.Elapsed.TotalSeconds;
                    RenderFrameInternal(_currentTime);

                    nextFrameTime = currentTime + targetFrameTime;

                    // Если сильно отстали, ресетим таймер
                    if (currentTime - nextFrameTime > targetFrameTime * 2)
                    {
                        nextFrameTime = currentTime + targetFrameTime;
                    }
                }
                else
                {
                    // Точная синхронизация
                    double sleepTime = nextFrameTime - currentTime;
                    if (sleepTime > 0.001)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(sleepTime * 0.5));
                    }
                    else
                    {
                        Thread.SpinWait(10);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in render loop: {ex.Message}");
                Thread.Sleep(5);
            }
        }

        Cleanup();
    }

    private void InitializeOpenGL()
    {
        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(_frameBufferWidth, _frameBufferHeight),
            Title = "Server Renderer",
            Flags = ContextFlags.Offscreen,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3)
        };

        _window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);
        _window.Context.MakeCurrent();
        _window.IsVisible = false;

        Console.WriteLine("OpenGL window created");

        // Компиляция шейдеров
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

        // Треугольник (позиция + цвет)
        float[] vertices =
        {
            0.0f, 0.5f, 0.0f, 1.0f, 0.0f, 0.0f,
            -0.5f, -0.5f, 0.0f, 0.0f, 1.0f, 0.0f,
            0.5f, -0.5f, 0.0f, 0.0f, 0.0f, 1.0f
        };

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsage.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);

        Console.WriteLine("OpenGL resources initialized");
    }

    public int Add(int a, int b)
    {
        return a + b;
    }

    private void RenderFrameInternal(float time)
    {
        try
        {
            _frameCount++;
            if (_frameCount % 200 == 0)
            {
                Console.WriteLine($"Rendering frame #{_frameCount} at time {time:F2}");
            }

            if (!_initialized || _window == null) return;

            // Читаем параметры (без lock для скорости - atomic reads)
            float scale = _triangleScale;
            float rotationSpeed = _rotationSpeed;
            Vector3 colorTint = _triangleColor;
            Vector2 position = _trianglePosition;

            // Очистка фона
            float bgR = (float)System.Math.Sin(time) * 0.2f + 0.2f;
            float bgG = (float)System.Math.Cos(time) * 0.2f + 0.2f;
            GL.ClearColor(bgR, bgG, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Установка uniform'ов
            GL.UseProgram(_shaderProgram);
            GL.Uniform1f(GL.GetUniformLocation(_shaderProgram, "uTime"), time);
            GL.Uniform1f(GL.GetUniformLocation(_shaderProgram, "uScale"), scale);
            GL.Uniform1f(GL.GetUniformLocation(_shaderProgram, "uRotationSpeed"), rotationSpeed);
            GL.Uniform3f(GL.GetUniformLocation(_shaderProgram, "uColorTint"), colorTint.X, colorTint.Y,
                colorTint.Z);
            GL.Uniform2f(GL.GetUniformLocation(_shaderProgram, "uPosition"), position.X, position.Y);

            // Рендеринг
            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(0);

            // Чтение пикселей (минимальная блокировка)
            int currentWrite = _writeIndex;
            GL.ReadPixels(0, 0, _frameBufferWidth, _frameBufferHeight,
                PixelFormat.Rgba, PixelType.UnsignedByte, _frameBuffer[currentWrite].PixelData);

            _frameBuffer[currentWrite].Timestamp = time;
            _writeIndex = (currentWrite + 1) % _frameBuffer.Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in RenderFrame: {ex.Message}");
        }
    }

    public Task<FrameData> GetFrameData(float requestedTime)
    {
        if (_disposed || !_initialized)
        {
            return Task.FromResult(new FrameData
            {
                Width = _frameBufferWidth,
                Height = _frameBufferHeight,
                PixelData = Array.Empty<byte>()
            });
        }

        // Быстрое чтение без блокировки записи
        int safeReadIndex = (_writeIndex - 1 + _frameBuffer.Length) % _frameBuffer.Length;
        var source = _frameBuffer[safeReadIndex];

        var result = new FrameData
        {
            Width = source.Width,
            Height = source.Height,
            Timestamp = source.Timestamp,
            PixelData = new byte[source.PixelData.Length]
        };

        Buffer.BlockCopy(source.PixelData, 0, result.PixelData, 0, source.PixelData.Length);

        return Task.FromResult(result);
    }

    public void OnKeyPress(string key)
    {
        switch (key.ToUpper())
        {
            case "W": _triangleScale = System.Math.Min(_triangleScale + 0.1f, 3.0f); break;
            case "S": _triangleScale = System.Math.Max(_triangleScale - 0.1f, 0.2f); break;
            case "A": _rotationSpeed = System.Math.Max(_rotationSpeed - 0.2f, 0.0f); break;
            case "D": _rotationSpeed = System.Math.Min(_rotationSpeed + 0.2f, 5.0f); break;
            case "R": _triangleColor = new Vector3(1.0f, 0.0f, 0.0f); break;
            case "G": _triangleColor = new Vector3(0.0f, 1.0f, 0.0f); break;
            case "B": _triangleColor = new Vector3(0.0f, 0.0f, 1.0f); break;
            case "SPACE":
                _triangleScale = 1.0f;
                _rotationSpeed = 1.0f;
                _triangleColor = new Vector3(1.0f, 1.0f, 1.0f);
                _trianglePosition = Vector2.Zero;
                break;
            case "UP": _trianglePosition.Y = System.Math.Min(_trianglePosition.Y + 0.1f, 0.8f); break;
            case "DOWN": _trianglePosition.Y = System.Math.Max(_trianglePosition.Y - 0.1f, -0.8f); break;
            case "LEFT": _trianglePosition.X = System.Math.Max(_trianglePosition.X - 0.1f, -0.8f); break;
            case "RIGHT": _trianglePosition.X = System.Math.Min(_trianglePosition.X + 0.1f, 0.8f); break;
        }
    }

    public void OnMouseMove(float normalizedX, float normalizedY)
    {
        _trianglePosition = new Vector2(normalizedX, normalizedY);
    }

    public void OnMouseClick(float normalizedX, float normalizedY, int button)
    {
        if (button == 0)
        {
            var random = new Random();
            _triangleColor = new Vector3(
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                (float)random.NextDouble()
            );
        }
        else if (button == 1)
        {
            _triangleScale = System.Math.Min(_triangleScale + 0.2f, 3.0f);
        }
    }

    private void Cleanup()
    {
        try
        {
            if (_vao != 0) GL.DeleteVertexArray(_vao);
            if (_vbo != 0) GL.DeleteBuffer(_vbo);
            if (_shaderProgram != 0) GL.DeleteProgram(_shaderProgram);
            _window?.Dispose();
            Console.WriteLine("OpenGL resources cleaned up");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Console.WriteLine("RenderServer disposed");
    }
}