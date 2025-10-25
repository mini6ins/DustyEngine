namespace Utils
{
    public enum KeyCode
    {
        Unknown,
        Space,
        Apostrophe,
        Comma,
        Minus,
        Period,
        Slash,
        D0,
        D1,
        D2,
        D3,
        D4,
        D5,
        D6,
        D7,
        D8,
        D9,
        Semicolon,
        Equal,
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,
        LeftBracket,
        Backslash,
        RightBracket,
        GraveAccent,
        World1,
        World2,
        Escape,
        Enter,
        Tab,
        Backspace,
        Insert,
        Delete,
        Right,
        Left,
        Down,
        Up,
        PageUp,
        PageDown,
        Home,
        End,
        CapsLock,
        ScrollLock,
        NumLock,
        PrintScreen,
        Pause,
        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,
        F13,
        F14,
        F15,
        F16,
        F17,
        F18,
        F19,
        F20,
        F21,
        F22,
        F23,
        F24,
        F25,
        KP0,
        KP1,
        KP2,
        KP3,
        KP4,
        KP5,
        KP6,
        KP7,
        KP8,
        KP9,
        KPDecimal,
        KPDivide,
        KPMultiply,
        KPSubtract,
        KPAdd,
        KPEnter,
        KPEqual,
        LeftShift,
        LeftControl,
        LeftAlt,
        LeftSuper,
        RightShift,
        RightControl,
        RightAlt,
        RightSuper,
        Menu
    }

 
    public static class Input
    {
        private static OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState? keyboardState;
        private static OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState? previousKeyboardState;
        private static readonly HashSet<KeyCode> TriggeredKeys = [];
        
        // ========== ДОБАВЬТЕ ЭТИ ПОЛЯ ==========
        private static readonly HashSet<OpenTK.Windowing.GraphicsLibraryFramework.Keys> _remoteKeysDown = new();
        private static readonly HashSet<OpenTK.Windowing.GraphicsLibraryFramework.Keys> _previousRemoteKeysDown = new();
        private static float _remoteMouseX = 0;
        private static float _remoteMouseY = 0;
        private static float _previousRemoteMouseX = 0;
        private static float _previousRemoteMouseY = 0;
        private static bool _useRemoteInput = false;
        // ========== КОНЕЦ ДОБАВЛЕНИЯ ==========
        
        public static void Update(OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState newKeyboardState)
        {
            // ========== ДОБАВЬТЕ ПРОВЕРКУ ==========
            // В удалённом режиме не обновляем локальное состояние
            if (!_useRemoteInput)
            {
                previousKeyboardState = keyboardState;
                keyboardState = newKeyboardState;
            }
            // ========== КОНЕЦ ИЗМЕНЕНИЯ ==========
    
            // Обновляем предыдущие состояния удалённого ввода
            _previousRemoteKeysDown.Clear();
            foreach (var key in _remoteKeysDown)
                _previousRemoteKeysDown.Add(key);
    
            _previousRemoteMouseX = _remoteMouseX;
            _previousRemoteMouseY = _remoteMouseY;
        }

        public static bool IsKeyDown(KeyCode keyCode)
        {
            // ========== ЗАМЕНИТЕ ЭТОТ МЕТОД ==========
            if (_useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return _remoteKeysDown.Contains(key);
            }
            
            if (keyboardState == null) return false;
            return keyboardState.IsKeyDown(ConvertKey(keyCode));
            // ========== КОНЕЦ ЗАМЕНЫ ==========
        }

        public static bool IsKeyPressed(KeyCode keyCode)
        {
            // ========== ЗАМЕНИТЕ ЭТОТ МЕТОД ==========
            if (_useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return _remoteKeysDown.Contains(key) && !_previousRemoteKeysDown.Contains(key);
            }
            
            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return keyboardState.IsKeyDown(k) && !previousKeyboardState.IsKeyDown(k);
            // ========== КОНЕЦ ЗАМЕНЫ ==========
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            // ========== ЗАМЕНИТЕ ЭТОТ МЕТОД ==========
            if (_useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return !_remoteKeysDown.Contains(key) && _previousRemoteKeysDown.Contains(key);
            }
            
            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return !keyboardState.IsKeyDown(k) && previousKeyboardState.IsKeyDown(k);
            // ========== КОНЕЦ ЗАМЕНЫ ==========
        }
        

        
        public static bool IsKeyJustActivatedOnce(KeyCode keyCode)
        {
            // ========== ЗАМЕНИТЕ ЭТОТ МЕТОД ==========
            if (_useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                
                if (_remoteKeysDown.Contains(key))
                {
                    if (!TriggeredKeys.Contains(keyCode))
                    {
                        TriggeredKeys.Add(keyCode);
                        return true;
                    }
                }
                else
                {
                    TriggeredKeys.Remove(keyCode);
                }
                return false;
            }
            
            if (keyboardState == null) return false;

            var k = ConvertKey(keyCode);
    
            if (keyboardState.IsKeyDown(k))
            {
                if (!TriggeredKeys.Contains(keyCode))
                {
                    TriggeredKeys.Add(keyCode);
                    return true;
                }
            }
            else
            {
                TriggeredKeys.Remove(keyCode);
            }

            return false;
            // ========== КОНЕЦ ЗАМЕНЫ ==========
        }

        private static OpenTK.Windowing.GraphicsLibraryFramework.Keys ConvertKey(KeyCode keyCode)
        {
            return Enum.TryParse(keyCode.ToString(), out OpenTK.Windowing.GraphicsLibraryFramework.Keys result)
                ? result
                : OpenTK.Windowing.GraphicsLibraryFramework.Keys.Unknown;
        }

        private static bool firstMove = true;
        private static float lastX;
        private static float lastY;
        private static (float X, float Y) currentDelta = (0, 0);

        public static (float X, float Y) Delta => currentDelta;

        public static void UpdateMouse(float x, float y)
        {
            // ========== ЗАМЕНИТЕ ЭТОТ МЕТОД ==========
            if (_useRemoteInput)
            {
                // Удалённый ввод уже в нормализованных координатах [0, 1]
                // Конвертируем в дельту
                currentDelta = (x - _remoteMouseX, y - _remoteMouseY);
                _remoteMouseX = x;
                _remoteMouseY = y;
                return;
            }
            
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
            // ========== КОНЕЦ ЗАМЕНЫ ==========
        }
        
        public static void ResetMouse()
        {
            currentDelta = (0, 0);
        }
        
        public static void FullResetMouse()
        {
            firstMove = true;
            currentDelta = (0, 0);
            
            // ========== ДОБАВЬТЕ ЭТО ==========
            _remoteMouseX = 0;
            _remoteMouseY = 0;
            _previousRemoteMouseX = 0;
            _previousRemoteMouseY = 0;
            // ========== КОНЕЦ ДОБАВЛЕНИЯ ==========
        }
        
        public static bool HasMouseMoved()
        {
            return Math.Abs(currentDelta.X) > 0.001f || Math.Abs(currentDelta.Y) > 0.001f;
        }

        public static float GetMouseSensitivityAdjustedDelta(float sensitivity)
        {
            return Math.Sqrt(currentDelta.X * currentDelta.X + currentDelta.Y * currentDelta.Y) * sensitivity;
        }

        // ========== ДОБАВЬТЕ ЭТИ МЕТОДЫ ==========
        
        /// <summary>
        /// Включить/выключить удалённый ввод
        /// </summary>
        public static void SetRemoteInputMode(bool enabled)
        {
            _useRemoteInput = enabled;
            if (enabled)
            {
                Console.WriteLine("[INPUT] Remote input mode ENABLED");
            }
            else
            {
                Console.WriteLine("[INPUT] Remote input mode DISABLED");
                _remoteKeysDown.Clear();
                _previousRemoteKeysDown.Clear();
            }
        }
        
        /// <summary>
        /// Обработать удалённое событие клавиатуры
        /// </summary>
        public static void ProcessRemoteKeyEvent(OpenTK.Windowing.GraphicsLibraryFramework.Keys key, bool isDown)
        {
            if (isDown)
            {
                _remoteKeysDown.Add(key);
            }
            else
            {
                _remoteKeysDown.Remove(key);
            }
        }
        
        /// <summary>
        /// Обработать удалённое событие мыши
        /// </summary>
        public static void ProcessRemoteMouseMove(float normalizedX, float normalizedY)
        {
            // Сохраняем нормализованные координаты [0, 1]
            // Для дельты вычисляем разницу
            float deltaX = normalizedX - _remoteMouseX;
            float deltaY = normalizedY - _remoteMouseY;
            
            _remoteMouseX = normalizedX;
            _remoteMouseY = normalizedY;
            currentDelta = (deltaX * 1000, deltaY * 1000); // Умножаем для чувствительности
        }
        
        // ========== КОНЕЦ ДОБАВЛЕНИЯ ==========
    }
}