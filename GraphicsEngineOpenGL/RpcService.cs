using DustyEngine.Runner;

namespace GraphicsEngineOpenGL;

/// <summary>
/// RPC Service для отдачи кадров и приема input от клиентов
/// </summary>
public class RpcService
{
    private readonly Func<FrameData> _getFrameData;
    private readonly Action<string, bool> _onKeyEvent;
    private readonly Action<float, float> _onMouseMove;
    private readonly Action<float, float, int, bool> _onMouseEvent;

    // ===== DEBUG SETTINGS =====
    public static bool EnableInputLogging { get; set; } = true; // Включено по умолчанию
    public static bool LogMouseMove { get; set; } = false; // Отключено по умолчанию (слишком много данных)
    
    private static readonly HashSet<string> _currentlyPressedKeys = new HashSet<string>();
    private static readonly HashSet<int> _currentlyPressedButtons = new HashSet<int>();
    private static readonly object _logLock = new object();

    public RpcService(
        Func<FrameData> getFrameData,
        Action<string, bool>? onKeyEvent = null,
        Action<float, float>? onMouseMove = null,
        Action<float, float, int, bool>? onMouseEvent = null)
    {
        _getFrameData = getFrameData;
        _onKeyEvent = onKeyEvent ?? ((_, _) => { });
        _onMouseMove = onMouseMove ?? ((_, _) => { });
        _onMouseEvent = onMouseEvent ?? ((_, _, _, _) => { });
    }

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

    public void OnKeyDown(string key)
    {
        try
        {
            if (EnableInputLogging)
            {
                lock (_logLock)
                {
                    if (_currentlyPressedKeys.Add(key))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[RPC INPUT] ⌨️  Key DOWN: {key}");
                        Console.ResetColor();
                        PrintCurrentState();
                    }
                }
            }
            
            _onKeyEvent(key, true);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RPC Service] ❌ Error in OnKeyDown: {ex.Message}");
            Console.ResetColor();
        }
    }

    public void OnKeyUp(string key)
    {
        try
        {
            if (EnableInputLogging)
            {
                lock (_logLock)
                {
                    if (_currentlyPressedKeys.Remove(key))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[RPC INPUT] ⌨️  Key UP: {key}");
                        Console.ResetColor();
                        PrintCurrentState();
                    }
                }
            }
            
            _onKeyEvent(key, false);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RPC Service] ❌ Error in OnKeyUp: {ex.Message}");
            Console.ResetColor();
        }
    }

    public void OnMouseMove(float normalizedX, float normalizedY)
    {
        try
        {
            if (EnableInputLogging && LogMouseMove)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[RPC INPUT] 🖱️  Mouse Move: X={normalizedX:F3}, Y={normalizedY:F3}");
                Console.ResetColor();
            }
            
            _onMouseMove(normalizedX, normalizedY);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RPC Service] ❌ Error in OnMouseMove: {ex.Message}");
            Console.ResetColor();
        }
    }

    public void OnMouseDown(float normalizedX, float normalizedY, int button)
    {
        try
        {
            if (EnableInputLogging)
            {
                lock (_logLock)
                {
                    if (_currentlyPressedButtons.Add(button))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[RPC INPUT] 🖱️  Mouse DOWN: Button={GetButtonName(button)}, X={normalizedX:F3}, Y={normalizedY:F3}");
                        Console.ResetColor();
                        PrintCurrentState();
                    }
                }
            }
            
            _onMouseEvent(normalizedX, normalizedY, button, true);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RPC Service] ❌ Error in OnMouseDown: {ex.Message}");
            Console.ResetColor();
        }
    }

    public void OnMouseUp(float normalizedX, float normalizedY, int button)
    {
        try
        {
            if (EnableInputLogging)
            {
                lock (_logLock)
                {
                    if (_currentlyPressedButtons.Remove(button))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[RPC INPUT] 🖱️  Mouse UP: Button={GetButtonName(button)}, X={normalizedX:F3}, Y={normalizedY:F3}");
                        Console.ResetColor();
                        PrintCurrentState();
                    }
                }
            }
            
            _onMouseEvent(normalizedX, normalizedY, button, false);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RPC Service] ❌ Error in OnMouseUp: {ex.Message}");
            Console.ResetColor();
        }
    }
    
    public void OnMouseMoveDelta(float deltaX, float deltaY)
    {
        try
        {
            if (EnableInputLogging && LogMouseMove && (System.Math.Abs(deltaX) > 0.01f || System.Math.Abs(deltaY) > 0.01f))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[RPC INPUT] 🖱️  Mouse Delta: ΔX={deltaX:F2}, ΔY={deltaY:F2}");
                Console.ResetColor();
            }
            
            _onMouseMove(deltaX, deltaY);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RPC Service] ❌ Error in OnMouseMoveDelta: {ex.Message}");
            Console.ResetColor();
        }
    }

    // ===== DEBUG HELPER METHODS =====
    
    private static void PrintCurrentState()
    {
        if (_currentlyPressedKeys.Count == 0 && _currentlyPressedButtons.Count == 0)
            return;

        var parts = new List<string>();
        
        if (_currentlyPressedKeys.Count > 0)
        {
            parts.Add($"Keys: [{string.Join(", ", _currentlyPressedKeys)}]");
        }
        
        if (_currentlyPressedButtons.Count > 0)
        {
            var buttonNames = _currentlyPressedButtons.Select(GetButtonName);
            parts.Add($"Mouse: [{string.Join(", ", buttonNames)}]");
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"      ├─ Active: {string.Join(" | ", parts)}");
        Console.ResetColor();
    }

    private static string GetButtonName(int button)
    {
        return button switch
        {
            0 => "Left",
            1 => "Right",
            2 => "Middle",
            _ => $"Button{button}"
        };
    }

    /// <summary>
    /// Получить текущее состояние всех нажатых клавиш и кнопок
    /// </summary>
    public static (string[] Keys, int[] MouseButtons) GetCurrentInputState()
    {
        lock (_logLock)
        {
            return (_currentlyPressedKeys.ToArray(), _currentlyPressedButtons.ToArray());
        }
    }

    /// <summary>
    /// Очистить состояние отладки (полезно при переподключении)
    /// </summary>
    public static void ClearDebugState()
    {
        lock (_logLock)
        {
            _currentlyPressedKeys.Clear();
            _currentlyPressedButtons.Clear();
        }
        
        if (EnableInputLogging)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[RPC INPUT] 🔄 Debug state cleared");
            Console.ResetColor();
        }
    }

    // Deprecated methods для обратной совместимости
    public void OnKeyPress(string key)
    {
        if (EnableInputLogging)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[RPC INPUT] ⚠️  Deprecated OnKeyPress called: {key}");
            Console.ResetColor();
        }
        
        _onKeyEvent(key, true);
        Task.Delay(50).ContinueWith(_ => _onKeyEvent(key, false));
    }

    public void OnMouseClick(float normalizedX, float normalizedY, int button)
    {
        if (EnableInputLogging)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[RPC INPUT] ⚠️  Deprecated OnMouseClick called: Button={GetButtonName(button)}");
            Console.ResetColor();
        }
        
        _onMouseEvent(normalizedX, normalizedY, button, true);
        Task.Delay(50).ContinueWith(_ => _onMouseEvent(normalizedX, normalizedY, button, false));
    }
}