using OpenTK.Windowing.GraphicsLibraryFramework;
using Utils;
using MouseButton = Utils.MouseButton;

namespace GraphicsEngineOpenGL.Input
{
    public static class Input
    {
        private static KeyboardState? _keyboardState;
        private static KeyboardState? _previousKeyboardState;
        private static MouseState? _mouseState;
        private static MouseState? _previousMouseState;

        private static readonly HashSet<KeyCode> TriggeredKeys = [];

        private static bool _firstMove = true;
        private static float _lastX;
        private static float _lastY;
        private static (float X, float Y) _currentDelta;
        
        private static readonly Dictionary<KeyCode, bool> RpcKeyStates = new();
        private static readonly Dictionary<MouseButton, bool> RpcMouseButtonStates = new();
        private static (float X, float Y) _rpcMouseDelta = (0f, 0f);
        private static readonly object RpcInputLock = new();

        public static (float X, float Y) Delta => IsRpcInputActive ? _rpcMouseDelta : _currentDelta;
        public static bool IsRpcInputActive { get; private set; }

        // Keyboard State Updates
        public static void Update(KeyboardState newKeyboardState)
        {
            _previousKeyboardState = _keyboardState;
            _keyboardState = newKeyboardState;
        }

        public static void UpdateMouseState(MouseState newMouseState)
        {
            _previousMouseState = _mouseState;
            _mouseState = newMouseState;
        }

        // Keyboard Input
        public static bool IsKeyDown(KeyCode keyCode)
        {
            if (!IsRpcInputActive)
                return _keyboardState?.IsKeyDown(ConvertKey(keyCode)) ?? false;
            
            lock (RpcInputLock)
            {
                return RpcKeyStates.TryGetValue(keyCode, out bool isPressed) && isPressed;
            }
        }

        public static bool IsKeyPressed(KeyCode keyCode)
        {
            if (IsRpcInputActive)
                return IsKeyDown(keyCode);
            
            if (_keyboardState == null || _previousKeyboardState == null)
                return false;
            
            var k = ConvertKey(keyCode);
            return _keyboardState.IsKeyDown(k) && !_previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            if (IsRpcInputActive)
                return !IsKeyDown(keyCode);
            
            if (_keyboardState == null || _previousKeyboardState == null)
                return false;
            
            var k = ConvertKey(keyCode);
            return !_keyboardState.IsKeyDown(k) && _previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyJustActivatedOnce(KeyCode keyCode)
        {
            if (IsRpcInputActive)
            {
                lock (RpcInputLock)
                {
                    if (RpcKeyStates.TryGetValue(keyCode, out var isPressed) && isPressed)
                    {
                        if (TriggeredKeys.Add(keyCode))
                            return true;
                    }
                    else
                    {
                        TriggeredKeys.Remove(keyCode);
                    }
                    return false;
                }
            }
            
            if (_keyboardState == null)
                return false;
            
            var k = ConvertKey(keyCode);

            if (_keyboardState.IsKeyDown(k))
            {
                if (TriggeredKeys.Add(keyCode))
                    return true;
            }
            else
            {
                TriggeredKeys.Remove(keyCode);
            }

            return false;
        }

        // Mouse Input
        public static bool IsMouseButtonDown(MouseButton button)
        {
            if (IsRpcInputActive)
            {
                lock (RpcInputLock)
                {
                    return RpcMouseButtonStates.TryGetValue(button, out var isPressed) && isPressed;
                }
            }
            
            return _mouseState != null && GetMouseButtonState(_mouseState, button);
        }

        public static bool IsMouseButtonPressed(MouseButton button)
        {
            if (IsRpcInputActive)
                return IsMouseButtonDown(button);
            
            if (_mouseState == null || _previousMouseState == null)
                return false;

            return GetMouseButtonState(_mouseState, button) && !GetMouseButtonState(_previousMouseState, button);
        }

        private static bool GetMouseButtonState(MouseState state, MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => state.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left),
                MouseButton.Right => state.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right),
                MouseButton.Middle => state.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle),
                _ => false
            };
        }

        // Mouse Movement
        public static void UpdateMouse(float x, float y)
        {
            if (_firstMove)
            {
                _lastX = x;
                _lastY = y;
                _firstMove = false;
                _currentDelta = (0, 0);
                return;
            }

            _currentDelta = (x - _lastX, y - _lastY);
            _lastX = x;
            _lastY = y;
        }

        public static void ResetMouse() => _currentDelta = (0, 0);

        public static bool HasMouseMoved() => 
            System.Math.Abs(_currentDelta.X) > 0.001f || System.Math.Abs(_currentDelta.Y) > 0.001f;

        // RPC Input Management
        public static void EnableRpcInput()
        {
            IsRpcInputActive = true;
            ClearRpcStates();
        }

        public static void DisableRpcInput()
        {
            IsRpcInputActive = false;
            ClearRpcStates();
        }

        private static void ClearRpcStates()
        {
            lock (RpcInputLock)
            {
                RpcKeyStates.Clear();
                RpcMouseButtonStates.Clear();
                _rpcMouseDelta = (0f, 0f);
            }
        }

        // RPC Keyboard Events
        public static void RpcKeyDown(string key)
        {
            if (!TryParseKeyCode(key, out KeyCode keyCode))
                return;
            
            lock (RpcInputLock)
            {
                RpcKeyStates[keyCode] = true;
            }
        }

        public static void RpcKeyUp(string key)
        {
            if (!TryParseKeyCode(key, out var keyCode))
                return;
            
            lock (RpcInputLock)
            {
                RpcKeyStates[keyCode] = false;
            }
        }

        // RPC Mouse Events
        public static void RpcMouseMove(float deltaX, float deltaY)
        {
            lock (RpcInputLock)
            {
                const float pixelScale = 0.5f;
                _rpcMouseDelta = (deltaX * pixelScale, deltaY * pixelScale);
            }
        }

        public static void RpcMouseDown(MouseButton button)
        {
            lock (RpcInputLock)
            {
                RpcMouseButtonStates[button] = true;
            }
        }

        public static void RpcMouseUp(MouseButton button)
        {
            lock (RpcInputLock)
            {
                RpcMouseButtonStates[button] = false;
            }
        }

        public static void RpcResetMouseDelta()
        {
            lock (RpcInputLock)
            {
                _rpcMouseDelta = (0f, 0f);
            }
        }

        // Helper Methods
        private static Keys ConvertKey(KeyCode keyCode) => 
            Enum.TryParse(keyCode.ToString(), out Keys result) ? result : Keys.Unknown;

        private static bool TryParseKeyCode(string key, out KeyCode keyCode)
        {
            keyCode = GetSpecialKeyCode(key.ToUpper());
            
            if (keyCode != KeyCode.Unknown || key.Length != 1)
                return keyCode != KeyCode.Unknown;
            
            var c = key.ToUpper()[0];
            return c switch
            {
                >= 'A' and <= 'Z' => Enum.TryParse(c.ToString(), out keyCode),
                >= '0' and <= '9' => Enum.TryParse("D" + c, out keyCode),
                _ => false
            };
        }

        private static KeyCode GetSpecialKeyCode(string key) => key switch
        {
            "SPACE" => KeyCode.Space,
            "ENTER" => KeyCode.Enter,
            "TAB" => KeyCode.Tab,
            "BACKSPACE" => KeyCode.Backspace,
            "DELETE" => KeyCode.Delete,
            "INSERT" => KeyCode.Insert,
            "HOME" => KeyCode.Home,
            "END" => KeyCode.End,
            "PAGEUP" => KeyCode.PageUp,
            "PAGEDOWN" => KeyCode.PageDown,
            "ESCAPE" => KeyCode.Escape,
            "UP" => KeyCode.Up,
            "DOWN" => KeyCode.Down,
            "LEFT" => KeyCode.Left,
            "RIGHT" => KeyCode.Right,
            "F1" => KeyCode.F1,
            "F2" => KeyCode.F2,
            "F3" => KeyCode.F3,
            "F4" => KeyCode.F4,
            "F5" => KeyCode.F5,
            "F6" => KeyCode.F6,
            "F7" => KeyCode.F7,
            "F8" => KeyCode.F8,
            "F9" => KeyCode.F9,
            "F10" => KeyCode.F10,
            "F11" => KeyCode.F11,
            "F12" => KeyCode.F12,
            "LEFTSHIFT" => KeyCode.LeftShift,
            "RIGHTSHIFT" => KeyCode.RightShift,
            "LEFTCONTROL" => KeyCode.LeftControl,
            "RIGHTCONTROL" => KeyCode.RightControl,
            "LEFTALT" => KeyCode.LeftAlt,
            "RIGHTALT" => KeyCode.RightAlt,
            "CAPSLOCK" => KeyCode.CapsLock,
            "SCROLLLOCK" => KeyCode.ScrollLock,
            "NUMLOCK" => KeyCode.NumLock,
            "PRINTSCREEN" => KeyCode.PrintScreen,
            "PAUSE" => KeyCode.Pause,
            "MENU" => KeyCode.Menu,
            "KP0" => KeyCode.KP0,
            "KP1" => KeyCode.KP1,
            "KP2" => KeyCode.KP2,
            "KP3" => KeyCode.KP3,
            "KP4" => KeyCode.KP4,
            "KP5" => KeyCode.KP5,
            "KP6" => KeyCode.KP6,
            "KP7" => KeyCode.KP7,
            "KP8" => KeyCode.KP8,
            "KP9" => KeyCode.KP9,
            "KPDECIMAL" => KeyCode.KPDecimal,
            "KPDIVIDE" => KeyCode.KPDivide,
            "KPMULTIPLY" => KeyCode.KPMultiply,
            "KPSUBTRACT" => KeyCode.KPSubtract,
            "KPADD" => KeyCode.KPAdd,
            "KPENTER" => KeyCode.KPEnter,
            "KPEQUAL" => KeyCode.KPEqual,
            "APOSTROPHE" => KeyCode.Apostrophe,
            "COMMA" => KeyCode.Comma,
            "MINUS" => KeyCode.Minus,
            "PERIOD" => KeyCode.Period,
            "SLASH" => KeyCode.Slash,
            "SEMICOLON" => KeyCode.Semicolon,
            "EQUAL" => KeyCode.Equal,
            "LEFTBRACKET" => KeyCode.LeftBracket,
            "BACKSLASH" => KeyCode.Backslash,
            "RIGHTBRACKET" => KeyCode.RightBracket,
            "GRAVEACCENT" => KeyCode.GraveAccent,
            _ => KeyCode.Unknown
        };
    }
}