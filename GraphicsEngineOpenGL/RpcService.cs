using DustyEngine.Runner;

namespace GraphicsEngineOpenGL;

/// <summary>
/// RPC Service для отдачи кадров клиентам
/// Изолирован от Window чтобы избежать конфликтов с OpenTK событиями
/// </summary>
public class RpcService
{
    private readonly Func<FrameData> _getFrameData;
    private readonly Action<string> _onKeyPress;
    private readonly Action<float, float> _onMouseMove;
    private readonly Action<float, float, int> _onMouseClick;

    public RpcService(
        Func<FrameData> getFrameData,
        Action<string>? onKeyPress = null,
        Action<float, float>? onMouseMove = null,
        Action<float, float, int>? onMouseClick = null)
    {
        _getFrameData = getFrameData;
        _onKeyPress = onKeyPress ?? (_ => { });
        _onMouseMove = onMouseMove ?? ((_, _) => { });
        _onMouseClick = onMouseClick ?? ((_, _, _) => { });
    }

    /// <summary>
    /// RPC метод: возвращает текущий кадр
    /// </summary>
    public Task<FrameData> GetFrameData(float requestedTime)
    {
        try
        {
            var frame = _getFrameData();
            return Task.FromResult(frame);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC Service] Error getting frame: {ex.Message}");
            return Task.FromResult(new FrameData
            {
                Width = 0,
                Height = 0,
                PixelData = Array.Empty<byte>()
            });
        }
    }

    /// <summary>
    /// RPC метод: обработка нажатия клавиши от клиента
    /// </summary>
    public void OnKeyPress(string key)
    {
        try
        {
            Console.WriteLine($"[RPC Service] Key pressed: {key}");
            _onKeyPress(key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC Service] Error in OnKeyPress: {ex.Message}");
        }
    }

    /// <summary>
    /// RPC метод: обработка движения мыши от клиента
    /// </summary>
    public void OnMouseMove(float normalizedX, float normalizedY)
    {
        try
        {
            _onMouseMove(normalizedX, normalizedY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC Service] Error in OnMouseMove: {ex.Message}");
        }
    }

    /// <summary>
    /// RPC метод: обработка клика мыши от клиента
    /// </summary>
    public void OnMouseClick(float normalizedX, float normalizedY, int button)
    {
        try
        {
            Console.WriteLine($"[RPC Service] Mouse click: ({normalizedX:F2}, {normalizedY:F2}), button: {button}");
            _onMouseClick(normalizedX, normalizedY, button);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC Service] Error in OnMouseClick: {ex.Message}");
        }
    }
}