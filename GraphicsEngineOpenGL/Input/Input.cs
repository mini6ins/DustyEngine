using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Utils
{
    public static class Input
    {
        private static KeyboardState? keyboardState;
        private static KeyboardState? previousKeyboardState;
        private static MouseState? mouseState;
        private static MouseState? previousMouseState;

        private static readonly HashSet<KeyCode> TriggeredKeys = [];

        private static bool firstMove = true;
        private static float lastX;
        private static float lastY;
        private static (float X, float Y) currentDelta;

        // ===== RPC INPUT STATE =====
        private static readonly Dictionary<KeyCode, bool> _rpcKeyStates = new Dictionary<KeyCode, bool>();
        private static readonly Dictionary<MouseButton, bool> _rpcMouseButtonStates = new Dictionary<MouseButton, bool>();
        private static (float X, float Y) _rpcMousePosition = (0f, 0f);
        private static (float X, float Y) _rpcMouseDelta = (0f, 0f);
        private static (float X, float Y) _lastRpcMousePosition = (0f, 0f);
        private static readonly object _rpcInputLock = new object();
        private static bool _useRpcInput = false;

        public static (float X, float Y) Delta => _useRpcInput ? _rpcMouseDelta : currentDelta;

        // ===== EXISTING LOCAL INPUT METHODS =====
        
        public static void Update(KeyboardState newKeyboardState)
        {
            previousKeyboardState = keyboardState;
            keyboardState = newKeyboardState;
        }

        public static void UpdateMouseState(MouseState newMouseState)
        {
            previousMouseState = mouseState;
            mouseState = newMouseState;
        }

        public static bool IsKeyDown(KeyCode keyCode)
        {
            if (_useRpcInput)
            {
                lock (_rpcInputLock)
                {
                    return _rpcKeyStates.TryGetValue(keyCode, out bool isPressed) && isPressed;
                }
            }
            return keyboardState != null && keyboardState.IsKeyDown(ConvertKey(keyCode));
        }

        public static bool IsKeyPressed(KeyCode keyCode)
        {
            if (_useRpcInput)
            {
                // RPC не поддерживает "pressed" (только down/up), возвращаем IsKeyDown
                return IsKeyDown(keyCode);
            }
            
            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return keyboardState.IsKeyDown(k) && !previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            if (_useRpcInput)
            {
                // RPC не поддерживает "released" (только down/up), возвращаем !IsKeyDown
                return !IsKeyDown(keyCode);
            }
            
            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return !keyboardState.IsKeyDown(k) && previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyJustActivatedOnce(KeyCode keyCode)
        {
            if (_useRpcInput)
            {
                lock (_rpcInputLock)
                {
                    if (_rpcKeyStates.TryGetValue(keyCode, out bool isPressed) && isPressed)
                    {
                        if (TriggeredKeys.Add(keyCode))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        TriggeredKeys.Remove(keyCode);
                    }
                    return false;
                }
            }
            
            if (keyboardState == null) return false;
            var k = ConvertKey(keyCode);

            if (keyboardState.IsKeyDown(k))
            {
                if (TriggeredKeys.Add(keyCode))
                {
                    return true;
                }
            }
            else
            {
                TriggeredKeys.Remove(keyCode);
            }

            return false;
        }

        public static bool IsMouseButtonDown(MouseButton button)
        {
            if (_useRpcInput)
            {
                lock (_rpcInputLock)
                {
                    return _rpcMouseButtonStates.TryGetValue(button, out bool isPressed) && isPressed;
                }
            }
            
            if (mouseState == null)
                return false;

            return button switch
            {
                MouseButton.Left => mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left),
                MouseButton.Right => mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right),
                MouseButton.Middle => mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle),
                _ => false
            };
        }

        public static bool IsMouseButtonPressed(MouseButton button)
        {
            if (_useRpcInput)
            {
                // RPC не поддерживает "pressed" (только down/up), возвращаем IsMouseButtonDown
                return IsMouseButtonDown(button);
            }
            
            if (mouseState == null || previousMouseState == null)
                return false;

            return button switch
            {
                MouseButton.Left =>
                    mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left) &&
                    !previousMouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left),
                MouseButton.Right => 
                    mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right) &&
                    !previousMouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right),
                MouseButton.Middle => 
                    mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle) &&
                    !previousMouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle),
                _ => false
            };
        }

        private static Keys ConvertKey(KeyCode keyCode) =>
            Enum.TryParse(keyCode.ToString(), out Keys result) ? result : Keys.Unknown;

        public static void UpdateMouse(float x, float y)
        {
            if (firstMove)
            {
                lastX = x;
                lastY = y;
                firstMove = false;
                currentDelta = (0, 0);
                return;
            }

            currentDelta = (x - lastX, y - lastY);

            lastX = x;
            lastY = y;
        }

        public static void ResetMouse() => currentDelta = (0, 0);

        public static bool HasMouseMoved() =>
            System.Math.Abs(currentDelta.X) > 0.001f || System.Math.Abs(currentDelta.Y) > 0.001f;

        // ===== RPC INPUT METHODS =====
        
        /// <summary>
        /// Включить режим RPC ввода (отключает локальную клавиатуру/мышь)
        /// </summary>
        public static void EnableRpcInput()
        {
            _useRpcInput = true;
            lock (_rpcInputLock)
            {
                _rpcKeyStates.Clear();
                _rpcMouseButtonStates.Clear();
                _rpcMouseDelta = (0f, 0f);
            }
        }

        /// <summary>
        /// Отключить режим RPC ввода (возвращается локальная клавиатура/мышь)
        /// </summary>
        public static void DisableRpcInput()
        {
            _useRpcInput = false;
            lock (_rpcInputLock)
            {
                _rpcKeyStates.Clear();
                _rpcMouseButtonStates.Clear();
                _rpcMouseDelta = (0f, 0f);
            }
        }

        /// <summary>
        /// Проверка активен ли режим RPC ввода
        /// </summary>
        public static bool IsRpcInputActive => _useRpcInput;

        /// <summary>
        /// Обработка нажатия клавиши из RPC
        /// </summary>
        public static void RpcKeyDown(string key)
        {
            if (TryParseKeyCode(key, out KeyCode keyCode))
            {
                lock (_rpcInputLock)
                {
                    _rpcKeyStates[keyCode] = true;
                }
            }
        }

        /// <summary>
        /// Обработка отпускания клавиши из RPC
        /// </summary>
        public static void RpcKeyUp(string key)
        {
            if (TryParseKeyCode(key, out KeyCode keyCode))
            {
                lock (_rpcInputLock)
                {
                    _rpcKeyStates[keyCode] = false;
                }
            }
        }

        /// <summary>
        /// Обработка движения мыши из RPC
        /// </summary>
        public static void RpcMouseMove(float deltaX, float deltaY)
        {
            lock (_rpcInputLock)
            {
                // ✅ Нормализуем пиксельную дельту к стандартному масштабу
                // Локальная мышь OpenTK обычно даёт значения ~1-10 пикселей на движение
                // RPC клиент может отправлять большие значения (50-200 пикселей)
                // Делим на коэффициент, чтобы привести к одному масштабу
                const float pixelScale = 0.5f; // Подбирайте: 0.3-0.7 обычно хорошо работает
        
                _rpcMouseDelta = (deltaX * pixelScale, deltaY * pixelScale);
            }
        }

        /// <summary>
        /// Обработка нажатия кнопки мыши из RPC
        /// </summary>
        public static void RpcMouseDown(MouseButton button)
        {
            lock (_rpcInputLock)
            {
                _rpcMouseButtonStates[button] = true;
            }
        }

        /// <summary>
        /// Обработка отпускания кнопки мыши из RPC
        /// </summary>
        public static void RpcMouseUp(MouseButton button)
        {
            lock (_rpcInputLock)
            {
                _rpcMouseButtonStates[button] = false;
            }
        }

        /// <summary>
        /// Сброс дельты мыши RPC (вызывать после применения)
        /// </summary>
        public static void RpcResetMouseDelta()
        {
            lock (_rpcInputLock)
            {
                _rpcMouseDelta = (0f, 0f);
            }
        }

        /// <summary>
        /// Получить текущую позицию мыши RPC
        /// </summary>
        public static (float X, float Y) GetRpcMousePosition()
        {
            lock (_rpcInputLock)
            {
                return _rpcMousePosition;
            }
        }

        private static bool TryParseKeyCode(string key, out KeyCode keyCode)
        {
            keyCode = key.ToUpper() switch
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
            
            // Если не нашли в switch, пробуем буквы A-Z и цифры 0-9
            if (keyCode == KeyCode.Unknown && key.Length == 1)
            {
                char c = key.ToUpper()[0];
                if (c >= 'A' && c <= 'Z')
                {
                    return Enum.TryParse(c.ToString(), out keyCode);
                }
                if (c >= '0' && c <= '9')
                {
                    return Enum.TryParse("D" + c, out keyCode);
                }
            }
            
            return keyCode != KeyCode.Unknown;
        }
    }
}