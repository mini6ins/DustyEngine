namespace Utils
{
    public static class Input
    {
        private static OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState? keyboardState;
        private static OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState? previousKeyboardState;
        private static readonly HashSet<KeyCode> TriggeredKeys = [];
        
        private static readonly HashSet<OpenTK.Windowing.GraphicsLibraryFramework.Keys> _remoteKeysDown = new();
        private static readonly HashSet<OpenTK.Windowing.GraphicsLibraryFramework.Keys> _previousRemoteKeysDown = new();
        private static bool _useRemoteInput = false;
        
        private static bool firstMove = true;
        private static float lastX;
        private static float lastY;
        private static (float X, float Y) currentDelta = (0, 0);
        
        private static float _accumulatedRemoteDeltaX = 0;
        private static float _accumulatedRemoteDeltaY = 0;
        private static readonly object _mouseLock = new object();

        public static (float X, float Y) Delta
        {
            get
            {
                if (_useRemoteInput)
                {
                    lock (_mouseLock)
                    {
                        return (currentDelta.X, currentDelta.Y);
                    }
                }

                return currentDelta;
            }
        }
        
        public static void Update()
        {
            if (_useRemoteInput)
            {
                _previousRemoteKeysDown.Clear();
                foreach (var key in _remoteKeysDown)
                    _previousRemoteKeysDown.Add(key);

     
                lock (_mouseLock)
                {
                    currentDelta = (_accumulatedRemoteDeltaX, _accumulatedRemoteDeltaY);
                    _accumulatedRemoteDeltaX = 0;
                    _accumulatedRemoteDeltaY = 0;
                }
            }
        }

        public static void Update(OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState newKeyboardState)
        {
            if (!_useRemoteInput)
            {
                previousKeyboardState = keyboardState;
                keyboardState = newKeyboardState;
            }

            _previousRemoteKeysDown.Clear();
            foreach (var key in _remoteKeysDown)
                _previousRemoteKeysDown.Add(key);
            
            if (_useRemoteInput)
            {
                lock (_mouseLock)
                {
                    currentDelta = (_accumulatedRemoteDeltaX, _accumulatedRemoteDeltaY);
                }
            }
        }

        public static bool IsKeyDown(KeyCode keyCode)
        {
            if (_useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return _remoteKeysDown.Contains(key);
            }

            if (keyboardState == null) return false;
            return keyboardState.IsKeyDown(ConvertKey(keyCode));
        }

        public static bool IsKeyPressed(KeyCode keyCode)
        {
            if (_useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return _remoteKeysDown.Contains(key) && !_previousRemoteKeysDown.Contains(key);
            }

            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return keyboardState.IsKeyDown(k) && !previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            if (_useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return !_remoteKeysDown.Contains(key) && _previousRemoteKeysDown.Contains(key);
            }

            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return !keyboardState.IsKeyDown(k) && previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyJustActivatedOnce(KeyCode keyCode)
        {
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
        }

        private static OpenTK.Windowing.GraphicsLibraryFramework.Keys ConvertKey(KeyCode keyCode)
        {
            return Enum.TryParse(keyCode.ToString(), out OpenTK.Windowing.GraphicsLibraryFramework.Keys result)
                ? result
                : OpenTK.Windowing.GraphicsLibraryFramework.Keys.Unknown;
        }

        public static void UpdateMouse(float x, float y)
        {
            if (_useRemoteInput)
                return;
            

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

        public static void ResetMouse()
        {
            if (_useRemoteInput)
            {
                lock (_mouseLock)
                {
                    _accumulatedRemoteDeltaX = 0;
                    _accumulatedRemoteDeltaY = 0;
                    currentDelta = (0, 0);
                }
            }
            else
            {
                currentDelta = (0, 0);
            }
        }

        public static void FullResetMouse()
        {
            firstMove = true;
            currentDelta = (0, 0);

            lock (_mouseLock)
            {
                _accumulatedRemoteDeltaX = 0;
                _accumulatedRemoteDeltaY = 0;
            }
        }

        public static bool HasMouseMoved()
        {
            return Math.Abs(currentDelta.X) > 0.001f || Math.Abs(currentDelta.Y) > 0.001f;
        }

        public static float GetMouseSensitivityAdjustedDelta(float sensitivity)
        {
            return Math.Sqrt(currentDelta.X * currentDelta.X + currentDelta.Y * currentDelta.Y) * sensitivity;
        }


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
        
        public static void ProcessRemoteMouseMove(float normalizedDeltaX, float normalizedDeltaY)
        {
            if (!_useRemoteInput) return;
            
            const float REFERENCE_WIDTH = 1280f;
            const float REFERENCE_HEIGHT = 720f;

            float deltaX = normalizedDeltaX * REFERENCE_WIDTH;
            float deltaY = normalizedDeltaY * REFERENCE_HEIGHT;
            
            lock (_mouseLock)
            {
                _accumulatedRemoteDeltaX += deltaX;
                _accumulatedRemoteDeltaY += deltaY;
            }
        }
    }
}