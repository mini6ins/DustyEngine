namespace GraphicsEngineOpenGL;

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

    public Task<FrameData> GetFrameData(float requestedTime)
    {
        return Task.FromResult(getFrameData());
    }

    public void OnKeyPress(string key)
    {
        _onKeyEvent(key, true);
        Task.Delay(50).ContinueWith(_ => _onKeyEvent(key, false));
    }

    public void OnKeyDown(string key)
    {
        lock (InputLock)
            CurrentlyPressedKeys.Add(key);

        _onKeyEvent(key, true);
    }

    public void OnKeyUp(string key)
    {
        lock (InputLock)
            CurrentlyPressedKeys.Remove(key);

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
        lock (InputLock)
            CurrentlyPressedButtons.Add(button);

        _onMouseEvent(normalizedX, normalizedY, button, true);
    }

    public void OnMouseUp(float normalizedX, float normalizedY, int button)
    {
        lock (InputLock)
            CurrentlyPressedButtons.Remove(button);

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