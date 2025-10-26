using OpenTK.Windowing.GraphicsLibraryFramework;
using Window = GraphicsEngineOpenGL.Window;

namespace Utils
{
    public static class Input
    {
        private static KeyboardState? keyboardState;
        private static KeyboardState? previousKeyboardState;
        private static readonly HashSet<KeyCode> TriggeredKeys = [];

        private static readonly HashSet<Keys> RemoteKeysDown = [];
        private static readonly HashSet<Keys> PreviousRemoteKeysDown = [];
        private static bool useRemoteInput;

        private static bool firstMove = true;
        private static float lastX;
        private static float lastY;
        private static (float X, float Y) currentDelta;

        private static float accumulatedRemoteDeltaX, accumulatedRemoteDeltaY;
        private static readonly object MouseLock = new();

        public static (float X, float Y) Delta => currentDelta;


        public static void Update()
        {
            if (!useRemoteInput) return;

            PreviousRemoteKeysDown.Clear();
            foreach (var key in RemoteKeysDown)
                PreviousRemoteKeysDown.Add(key);

            lock (MouseLock)
            {
                currentDelta = (accumulatedRemoteDeltaX, accumulatedRemoteDeltaY);
                accumulatedRemoteDeltaX = 0;
                accumulatedRemoteDeltaY = 0;
            }
        }

        public static void Update(KeyboardState newKeyboardState)
        {
            if (useRemoteInput) return;

            previousKeyboardState = keyboardState;
            keyboardState = newKeyboardState;
        }

        public static bool IsKeyDown(KeyCode keyCode)
        {
            if (useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return RemoteKeysDown.Contains(key);
            }

            return keyboardState != null && keyboardState.IsKeyDown(ConvertKey(keyCode));
        }

        public static bool IsKeyPressed(KeyCode keyCode)
        {
            if (useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return RemoteKeysDown.Contains(key) && !PreviousRemoteKeysDown.Contains(key);
            }

            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return keyboardState.IsKeyDown(k) && !previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            if (useRemoteInput)
            {
                var key = ConvertKey(keyCode);
                return !RemoteKeysDown.Contains(key) && PreviousRemoteKeysDown.Contains(key);
            }

            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return !keyboardState.IsKeyDown(k) && previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyJustActivatedOnce(KeyCode keyCode)
        {
            if (useRemoteInput)
            {
                var key = ConvertKey(keyCode);

                if (RemoteKeysDown.Contains(key))
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

        private static Keys ConvertKey(KeyCode keyCode)
        {
            return Enum.TryParse(keyCode.ToString(), out Keys result) ? result : Keys.Unknown;
        }

        public static void UpdateMouse(float x, float y)
        {
            if (useRemoteInput)
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
            currentDelta = (0, 0);
        }

        private static void FullResetMouse()
        {
            firstMove = true;
            currentDelta = (0, 0);

            lock (MouseLock)
            {
                accumulatedRemoteDeltaX = 0;
                accumulatedRemoteDeltaY = 0;
            }
        }

        public static bool HasMouseMoved()
        {
            return Math.Math.Abs(currentDelta.X) > 0.001f || Math.Math.Abs(currentDelta.Y) > 0.001f;
        }

        public static float GetMouseSensitivityAdjustedDelta(float sensitivity)
        {
            return Math.Math.Sqrt(currentDelta.X * currentDelta.X + currentDelta.Y * currentDelta.Y) * sensitivity;
        }

        public static void SetRemoteInputMode(bool enabled)
        {
            useRemoteInput = enabled;
            if (enabled)
            {
                FullResetMouse();
            }
            else
            {
                RemoteKeysDown.Clear();
                PreviousRemoteKeysDown.Clear();
                FullResetMouse();
            }
        }

        public static void ProcessRemoteKeyEvent(Keys key, bool isDown)
        {
            if (isDown)
                RemoteKeysDown.Add(key);
            else
                RemoteKeysDown.Remove(key);
        }

        public static void ProcessRemoteMouseMove(float normalizedDeltaX, float normalizedDeltaY)
        {
            if (!useRemoteInput) return;

            float deltaX = normalizedDeltaX * Window.ContextFramebufferWidth;
            float deltaY = normalizedDeltaY * Window.ContextFramebufferHeight;

            lock (MouseLock)
            {
                accumulatedRemoteDeltaX += deltaX;
                accumulatedRemoteDeltaY += deltaY;
            }
        }
    }
}