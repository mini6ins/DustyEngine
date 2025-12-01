using System.IO.Pipes;
using OpenTK.Graphics.OpenGL.Compatibility;
using StreamJsonRpc;
using Utils;
using Buffer = System.Buffer;

namespace GraphicsEngineOpenGL;

public class RpcController : IDisposable
{
    private readonly FrameSlot[] _frameSlots = new FrameSlot[2];
    private volatile int _latestFrameIndex;
    private readonly System.Diagnostics.Stopwatch _frameStopwatch = System.Diagnostics.Stopwatch.StartNew();
    private bool _initialized;

    private Thread? _rpcServerThread;
    private volatile bool _rpcServerRunning;
    private int _connectedClients;

    public int ConnectedClients => _connectedClients;

    public RpcController(int width, int height)
    {
        InitializeFrameBuffers(width, height);
    }

    private void InitializeFrameBuffers(int width, int height)
    {
        var pixelCount = width * height * 4;

        for (var i = 0; i < _frameSlots.Length; i++)
        {
            _frameSlots[i] = new FrameSlot
            {
                Frame = new FrameData
                {
                    Width = width,
                    Height = height,
                    PixelData = new byte[pixelCount]
                }
            };
        }

        _initialized = true;
    }

    public void CaptureFrame(int width, int height)
    {
        if (!_initialized) return;

        try
        {
            var writeIndex = 1 - _latestFrameIndex;
            var slot = _frameSlots[writeIndex];
            var targetBuffer = slot.Frame;

            var expectedSize = width * height * 4;

            if (targetBuffer.PixelData.Length != expectedSize)
                targetBuffer.PixelData = new byte[expectedSize];

            GL.ReadPixels(0, 0, width, height,
                PixelFormat.Rgba, PixelType.UnsignedByte, targetBuffer.PixelData);

            targetBuffer.Timestamp = (float)_frameStopwatch.Elapsed.TotalSeconds;
            targetBuffer.Width = width;
            targetBuffer.Height = height;

            slot.IsReady = true;
            _latestFrameIndex = writeIndex;
        }
        catch (Exception)
        {
        }
    }

    private FrameData GetCurrentFrame(int fallbackWidth, int fallbackHeight)
    {
        if (!_initialized)
        {
            return new FrameData
            {
                Width = fallbackWidth,
                Height = fallbackHeight,
                PixelData = Array.Empty<byte>()
            };
        }

        var slot = _frameSlots[_latestFrameIndex];

        if (!slot.IsReady)
        {
            return new FrameData
            {
                Width = fallbackWidth,
                Height = fallbackHeight,
                PixelData = Array.Empty<byte>()
            };
        }

        var source = slot.Frame;
        var result = new FrameData
        {
            Width = source.Width,
            Height = source.Height,
            Timestamp = source.Timestamp,
            PixelData = new byte[source.PixelData.Length]
        };

        Buffer.BlockCopy(source.PixelData, 0, result.PixelData, 0, source.PixelData.Length);
        return result;
    }

    public void Start()
    {
        if (_rpcServerRunning) return;

        _rpcServerRunning = true;
        _rpcServerThread = new Thread(RpcServerLoop)
        {
            Name = "RPC Server Thread",
            IsBackground = true
        };
        _rpcServerThread.Start();
    }

    public void Stop()
    {
        if (!_rpcServerRunning) return;

        _rpcServerRunning = false;
        _rpcServerThread?.Join(TimeSpan.FromSeconds(2));
    }

    private async void RpcServerLoop()
    {
        var clientId = 0;
        while (_rpcServerRunning)
        {
            try
            {
                var stream = new NamedPipeServerStream(
                    "StreamJsonRpcSamplePipe",
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await stream.WaitForConnectionAsync();
                int currentClientId = ++clientId;
                _connectedClients++;

                _ = Task.Run(() => HandleRpcClient(stream, currentClientId));
            }
            catch (Exception)
            {
                await Task.Delay(1000);
            }
        }
    }

    private async Task HandleRpcClient(NamedPipeServerStream stream, int clientId)
    {
        try
        {
            await using (stream)
            {
                var rpcService = new RpcService(
                    getFrameData: () => GetCurrentFrame(800, 600),
                    onKeyEvent: HandleKeyEvent,
                    onMouseMove: HandleMouseMove,
                    onMouseEvent: HandleMouseEvent
                );

                var jsonRpc = JsonRpc.Attach(stream, rpcService);

                jsonRpc.Disconnected += (sender, args) =>
                {
                    _connectedClients--;
                };

                await jsonRpc.Completion;
            }
        }
        catch (Exception)
        {
            _connectedClients--;
        }
    }

    private static void HandleKeyEvent(string key, bool isPressed)
    {
        if (isPressed)
            Input.Input.RpcKeyDown(key);
        else
            Input.Input.RpcKeyUp(key);
    }

    private static void HandleMouseMove(float normalizedX, float normalizedY)
    {
        Input.Input.RpcMouseMove(normalizedX, normalizedY);
    }

    private static void HandleMouseEvent(float normalizedX, float normalizedY, int button, bool isPressed)
    {
        var mouseButton = button switch
        {
            0 => MouseButton.Left,
            1 => MouseButton.Right,
            2 => MouseButton.Middle,
            _ => MouseButton.Left
        };

        if (isPressed)
            Input.Input.RpcMouseDown(mouseButton);
        else
            Input.Input.RpcMouseUp(mouseButton);
    }

    public void Dispose()
    {
        Stop();
    }
}

public class FrameSlot
{
    public FrameData Frame = new();
    public volatile bool IsReady = false;
}

public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public float Timestamp { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}

public class RpcService(
    Func<FrameData> getFrameData,
    Action<string, bool>? onKeyEvent = null,
    Action<float, float>? onMouseMove = null,
    Action<float, float, int, bool>? onMouseEvent = null)
{
    private readonly Action<string, bool> _onKeyEvent = onKeyEvent ?? ((_, _) => { });
    private readonly Action<float, float> _onMouseMove = onMouseMove ?? ((_, _) => { });
    private readonly Action<float, float, int, bool> _onMouseEvent = onMouseEvent ?? ((_, _, _, _) => { });

    private static readonly HashSet<string> CurrentlyPressedKeys = [];
    private static readonly HashSet<int> CurrentlyPressedButtons = [];
    private static readonly object InputLock = new();

    public Task<FrameData> GetFrameData(float requestedTime) => Task.FromResult(getFrameData());

    public void OnKeyPress(string key)
    {
        _onKeyEvent(key, true);
        Task.Delay(50).ContinueWith(_ => _onKeyEvent(key, false));
    }

    public void OnKeyDown(string key)
    {
        lock (InputLock) CurrentlyPressedKeys.Add(key);
        _onKeyEvent(key, true);
    }

    public void OnKeyUp(string key)
    {
        lock (InputLock) CurrentlyPressedKeys.Remove(key);
        _onKeyEvent(key, false);
    }

    public void OnMouseMove(float normalizedX, float normalizedY) => _onMouseMove(normalizedX, normalizedY);
    public void OnMouseMoveDelta(float deltaX, float deltaY) => _onMouseMove(deltaX, deltaY);

    public void OnMouseClick(float normalizedX, float normalizedY, int button)
    {
        _onMouseEvent(normalizedX, normalizedY, button, true);
        Task.Delay(50).ContinueWith(_ => _onMouseEvent(normalizedX, normalizedY, button, false));
    }

    public void OnMouseDown(float normalizedX, float normalizedY, int button)
    {
        lock (InputLock) CurrentlyPressedButtons.Add(button);
        _onMouseEvent(normalizedX, normalizedY, button, true);
    }

    public void OnMouseUp(float normalizedX, float normalizedY, int button)
    {
        lock (InputLock) CurrentlyPressedButtons.Remove(button);
        _onMouseEvent(normalizedX, normalizedY, button, false);
    }

    public static (string[] Keys, int[] MouseButtons) GetCurrentInputState()
    {
        lock (InputLock)
        {
            return (CurrentlyPressedKeys.ToArray(), CurrentlyPressedButtons.ToArray());
        }
    }

    public static void ClearDebugState()
    {
        lock (InputLock)
        {
            CurrentlyPressedKeys.Clear();
            CurrentlyPressedButtons.Clear();
        }
    }
}