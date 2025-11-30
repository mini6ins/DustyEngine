using DustyEngineEditor.Panels.RemoteRenderer;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2 = System.Numerics.Vector2;

namespace DustyEngineEditor;

public class InputHandler(IRemoteRenderer remoteRenderer)
{
    private readonly HashSet<ImGuiKey> _pressedKeys = [];
    private readonly HashSet<ImGuiMouseButton> _pressedMouseButtons = [];
    private readonly HashSet<Keys> _pressedRawKeys = [];
    private Vector2 _lastMousePos = Vector2.Zero;

    private static readonly ImGuiKey[] KeysToCheck =
    [
        // Letters
        ImGuiKey.A, ImGuiKey.B, ImGuiKey.C, ImGuiKey.D, ImGuiKey.E, ImGuiKey.F, ImGuiKey.G, ImGuiKey.H,
        ImGuiKey.I, ImGuiKey.J, ImGuiKey.K, ImGuiKey.L, ImGuiKey.M, ImGuiKey.N, ImGuiKey.O, ImGuiKey.P,
        ImGuiKey.Q, ImGuiKey.R, ImGuiKey.S, ImGuiKey.T, ImGuiKey.U, ImGuiKey.V, ImGuiKey.W, ImGuiKey.X,
        ImGuiKey.Y, ImGuiKey.Z,
        
        // Numbers
        ImGuiKey._0, ImGuiKey._1, ImGuiKey._2, ImGuiKey._3, ImGuiKey._4,
        ImGuiKey._5, ImGuiKey._6, ImGuiKey._7, ImGuiKey._8, ImGuiKey._9,
        
        // Special keys
        ImGuiKey.Space, ImGuiKey.Enter, ImGuiKey.Tab, ImGuiKey.Backspace, ImGuiKey.Delete,
        ImGuiKey.Insert, ImGuiKey.Home, ImGuiKey.End, ImGuiKey.PageUp, ImGuiKey.PageDown, ImGuiKey.Escape,
        
        // Arrows
        ImGuiKey.UpArrow, ImGuiKey.DownArrow, ImGuiKey.LeftArrow, ImGuiKey.RightArrow,
        
        // Modifiers
        ImGuiKey.LeftShift, ImGuiKey.RightShift, ImGuiKey.LeftCtrl, ImGuiKey.RightCtrl,
        ImGuiKey.LeftAlt, ImGuiKey.RightAlt, ImGuiKey.LeftSuper, ImGuiKey.RightSuper,
        
        // Lock keys
        ImGuiKey.CapsLock, ImGuiKey.ScrollLock, ImGuiKey.NumLock, ImGuiKey.PrintScreen,
        ImGuiKey.Pause, ImGuiKey.Menu,
        
        // Numpad
        ImGuiKey.Keypad0, ImGuiKey.Keypad1, ImGuiKey.Keypad2, ImGuiKey.Keypad3, ImGuiKey.Keypad4,
        ImGuiKey.Keypad5, ImGuiKey.Keypad6, ImGuiKey.Keypad7, ImGuiKey.Keypad8, ImGuiKey.Keypad9,
        ImGuiKey.KeypadDecimal, ImGuiKey.KeypadDivide, ImGuiKey.KeypadMultiply,
        ImGuiKey.KeypadSubtract, ImGuiKey.KeypadAdd, ImGuiKey.KeypadEnter, ImGuiKey.KeypadEqual
    ];

    private static readonly Keys[] FunctionKeys =
    [
        Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6,
        Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12
    ];

    private static readonly (ImGuiMouseButton Button, int Index)[] MouseButtons =
    [
        (ImGuiMouseButton.Left, 0),
        (ImGuiMouseButton.Right, 1),
        (ImGuiMouseButton.Middle, 2)
    ];

    private static readonly Dictionary<ImGuiKey, string> KeyNames = new()
    {
        // Letters
        { ImGuiKey.A, "A" }, { ImGuiKey.B, "B" }, { ImGuiKey.C, "C" }, { ImGuiKey.D, "D" },
        { ImGuiKey.E, "E" }, { ImGuiKey.F, "F" }, { ImGuiKey.G, "G" }, { ImGuiKey.H, "H" },
        { ImGuiKey.I, "I" }, { ImGuiKey.J, "J" }, { ImGuiKey.K, "K" }, { ImGuiKey.L, "L" },
        { ImGuiKey.M, "M" }, { ImGuiKey.N, "N" }, { ImGuiKey.O, "O" }, { ImGuiKey.P, "P" },
        { ImGuiKey.Q, "Q" }, { ImGuiKey.R, "R" }, { ImGuiKey.S, "S" }, { ImGuiKey.T, "T" },
        { ImGuiKey.U, "U" }, { ImGuiKey.V, "V" }, { ImGuiKey.W, "W" }, { ImGuiKey.X, "X" },
        { ImGuiKey.Y, "Y" }, { ImGuiKey.Z, "Z" },

        // Numbers
        { ImGuiKey._0, "0" }, { ImGuiKey._1, "1" }, { ImGuiKey._2, "2" },
        { ImGuiKey._3, "3" }, { ImGuiKey._4, "4" }, { ImGuiKey._5, "5" },
        { ImGuiKey._6, "6" }, { ImGuiKey._7, "7" }, { ImGuiKey._8, "8" }, { ImGuiKey._9, "9" },

        // Special keys
        { ImGuiKey.Space, "SPACE" }, { ImGuiKey.Enter, "ENTER" }, { ImGuiKey.Tab, "TAB" },
        { ImGuiKey.Backspace, "BACKSPACE" }, { ImGuiKey.Delete, "DELETE" },
        { ImGuiKey.Insert, "INSERT" }, { ImGuiKey.Home, "HOME" }, { ImGuiKey.End, "END" },
        { ImGuiKey.PageUp, "PAGEUP" }, { ImGuiKey.PageDown, "PAGEDOWN" },
        { ImGuiKey.Escape, "ESCAPE" },

        // Arrows
        { ImGuiKey.UpArrow, "UP" }, { ImGuiKey.DownArrow, "DOWN" },
        { ImGuiKey.LeftArrow, "LEFT" }, { ImGuiKey.RightArrow, "RIGHT" },

        // F-keys
        { ImGuiKey.F1, "F1" }, { ImGuiKey.F2, "F2" }, { ImGuiKey.F3, "F3" }, { ImGuiKey.F4, "F4" },
        { ImGuiKey.F5, "F5" }, { ImGuiKey.F6, "F6" }, { ImGuiKey.F7, "F7" }, { ImGuiKey.F8, "F8" },
        { ImGuiKey.F9, "F9" }, { ImGuiKey.F10, "F10" }, { ImGuiKey.F11, "F11" }, { ImGuiKey.F12, "F12" },

        // Modifiers
        { ImGuiKey.LeftShift, "LEFTSHIFT" }, { ImGuiKey.RightShift, "RIGHTSHIFT" },
        { ImGuiKey.LeftCtrl, "LEFTCONTROL" }, { ImGuiKey.RightCtrl, "RIGHTCONTROL" },
        { ImGuiKey.LeftAlt, "LEFTALT" }, { ImGuiKey.RightAlt, "RIGHTALT" },

        // Numpad
        { ImGuiKey.Keypad0, "KP0" }, { ImGuiKey.Keypad1, "KP1" }, { ImGuiKey.Keypad2, "KP2" },
        { ImGuiKey.Keypad3, "KP3" }, { ImGuiKey.Keypad4, "KP4" }, { ImGuiKey.Keypad5, "KP5" },
        { ImGuiKey.Keypad6, "KP6" }, { ImGuiKey.Keypad7, "KP7" }, { ImGuiKey.Keypad8, "KP8" },
        { ImGuiKey.Keypad9, "KP9" },
        { ImGuiKey.KeypadAdd, "KPADD" }, { ImGuiKey.KeypadSubtract, "KPSUBTRACT" },
        { ImGuiKey.KeypadMultiply, "KPMULTIPLY" }, { ImGuiKey.KeypadDivide, "KPDIVIDE" },
        { ImGuiKey.KeypadEnter, "KPENTER" },

        // Symbols
        { ImGuiKey.Comma, "COMMA" }, { ImGuiKey.Period, "PERIOD" },
        { ImGuiKey.Slash, "SLASH" }, { ImGuiKey.Semicolon, "SEMICOLON" },
        { ImGuiKey.Minus, "MINUS" }, { ImGuiKey.Equal, "EQUAL" }
    };

    public void ProcessKeyboard()
    {
        foreach (var key in KeysToCheck)
        {
            ProcessKey(key, ImGui.IsKeyDown(key), _pressedKeys, remoteRenderer.OnKeyDown, remoteRenderer.OnKeyUp);
        }
    }

    public void ProcessFunctionKeys(KeyboardState keyboardState)
    {
        foreach (var key in FunctionKeys)
        {
            ProcessKey(key, keyboardState.IsKeyDown(key), _pressedRawKeys, remoteRenderer.OnKeyDown, remoteRenderer.OnKeyUp);
        }
    }

    public void ProcessMouse(Vector2 imageSize, Vector2 imagePos)
    {
        ProcessMouseMovement();
        ProcessMouseButtons(imageSize, imagePos);
    }

    private void ProcessMouseMovement()
    {
        var mousePos = ImGui.GetMousePos();
        var deltaX = mousePos.X - _lastMousePos.X;
        var deltaY = mousePos.Y - _lastMousePos.Y;

        if (!(Math.Abs(deltaX) > 0.1f) && !(Math.Abs(deltaY) > 0.1f)) return;
        
        _lastMousePos = mousePos;
        SendToRemote(() => remoteRenderer.OnMouseMoveDelta(deltaX, deltaY));
    }

    private void ProcessMouseButtons(Vector2 imageSize, Vector2 imagePos)
    {
        var mousePos = ImGui.GetMousePos();
        var normalizedX = ((mousePos.X - imagePos.X) / imageSize.X) * 2.0f - 1.0f;
        var normalizedY = -(((mousePos.Y - imagePos.Y) / imageSize.Y) * 2.0f - 1.0f);

        foreach (var (button, index) in MouseButtons)
        {
            var isDown = ImGui.IsMouseDown(button);
            var wasPressed = _pressedMouseButtons.Contains(button);

            switch (isDown)
            {
                case true when !wasPressed:
                    _pressedMouseButtons.Add(button);
                    SendToRemote(() => remoteRenderer.OnMouseDown(normalizedX, normalizedY, index));
                    break;
                case false when wasPressed:
                    _pressedMouseButtons.Remove(button);
                    SendToRemote(() => remoteRenderer.OnMouseUp(normalizedX, normalizedY, index));
                    break;
            }
        }
    }

    private void ProcessKey<T>(T key, bool isDown, HashSet<T> pressedKeys, 
        Action<string> onKeyDown, Action<string> onKeyUp) where T : notnull
    {
        var wasPressed = pressedKeys.Contains(key);

        switch (isDown)
        {
            case true when !wasPressed:
            {
                pressedKeys.Add(key);
                var keyName = GetKeyName(key);
                SendToRemote(() => onKeyDown(keyName));
                break;
            }
            case false when wasPressed:
            {
                pressedKeys.Remove(key);
                var keyName = GetKeyName(key);
                SendToRemote(() => onKeyUp(keyName));
                break;
            }
        }
    }

    private static string GetKeyName<T>(T key)
    {
        return key switch
        {
            ImGuiKey imGuiKey => KeyNames.GetValueOrDefault(imGuiKey, imGuiKey.ToString().ToUpper()),
            Keys rawKey => rawKey.ToString().ToUpper(),
            _ => key.ToString()!.ToUpper()
        };
    }

    private static void SendToRemote(Action action)
    {
        Task.Run(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INPUT] Error: {ex.Message}");
            }
        });
    }
}