using System.Diagnostics;
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
    private readonly Stopwatch _frameStopwatch = Stopwatch.StartNew();
    private bool _initialized;

    private Thread? _rpcServerThread;
    private volatile bool _rpcServerRunning;

    public int ConnectedClients { get; private set; }

    public RpcController(int width, int height) => InitializeFrameBuffers(width, height);

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
            // ignored
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
                PixelData = []
            };
        }

        var slot = _frameSlots[_latestFrameIndex];

        if (!slot.IsReady)
        {
            return new FrameData
            {
                Width = fallbackWidth,
                Height = fallbackHeight,
                PixelData = []
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
                var currentClientId = ++clientId;
                ConnectedClients++;

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
                var callbacks = new RpcCallbacks
                {
                    OnKeyEvent = HandleKeyEvent,
                    OnMouseMove = HandleMouseMove,
                    OnMouseEvent = HandleMouseEvent,
                    OnPlayEngine = HandlePlayEngine,
                    OnStopEngine = HandleStopEngine,
                };

                var rpcService = new RpcService(() => GetCurrentFrame(800, 600), callbacks);

                var jsonRpc = JsonRpc.Attach(stream, rpcService);

                jsonRpc.Disconnected += (_, _) => ConnectedClients--;

                await jsonRpc.Completion;
            }
        }
        catch (Exception)
        {
            ConnectedClients--;
        }
    }


    private static void HandlePlayEngine()
    {
        Console.WriteLine("[ENGINE] ▶️ PLAY ENGINE - Game Started!");
    }

    private static void HandleStopEngine()
    {
        Console.WriteLine("[ENGINE] ⏹️ STOP ENGINE - Game Stopped!");
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

    public void Dispose() => Stop();
}

public class RpcService(Func<FrameData> getFrameData, RpcCallbacks callbacks)
{
    public Task<FrameData> GetFrameData(float requestedTime) => Task.FromResult(getFrameData());

    public void OnKeyDown(string key) => callbacks.OnKeyEvent?.Invoke(key, true);
    public void OnKeyUp(string key) => callbacks.OnKeyEvent?.Invoke(key, false);
    public void OnMouseMoveDelta(float deltaX, float deltaY) => callbacks.OnMouseMove?.Invoke(deltaX, deltaY);

    public void OnMouseDown(float normalizedX, float normalizedY, int button) =>
        callbacks.OnMouseEvent?.Invoke(normalizedX, normalizedY, button, true);

    public void OnMouseUp(float normalizedX, float normalizedY, int button) =>
        callbacks.OnMouseEvent?.Invoke(normalizedX, normalizedY, button, false);


    public void PlayEngine() => callbacks.OnPlayEngine?.Invoke();
    public void StopEngine() => callbacks.OnStopEngine?.Invoke();
}
