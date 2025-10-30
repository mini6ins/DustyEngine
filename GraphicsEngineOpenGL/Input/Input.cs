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

        public static (float X, float Y) Delta => currentDelta;

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

        public static bool IsKeyDown(KeyCode keyCode) =>
            keyboardState != null && keyboardState.IsKeyDown(ConvertKey(keyCode));


        public static bool IsKeyPressed(KeyCode keyCode)
        {
            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return keyboardState.IsKeyDown(k) && !previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            if (keyboardState == null || previousKeyboardState == null) return false;
            var k = ConvertKey(keyCode);
            return !keyboardState.IsKeyDown(k) && previousKeyboardState.IsKeyDown(k);
        }

        public static bool IsKeyJustActivatedOnce(KeyCode keyCode)
        {
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
            if (mouseState == null)
                return false;

            return button switch
            {
                MouseButton.Left => mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left),
                MouseButton.Right => mouseState.IsButtonDown(
                    OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right),
                MouseButton.Middle => mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton
                    .Middle),
                _ => false
            };
        }

        public static bool IsMouseButtonPressed(MouseButton button)
        {
            if (mouseState == null || previousMouseState == null)
                return false;

            return button switch
            {
                MouseButton.Left =>
                    mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left) &&
                    !previousMouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left),
                MouseButton.Right => mouseState.IsButtonDown(
                                         OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right) &&
                                     !previousMouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right),
                MouseButton.Middle => mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton
                                          .Middle) &&
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
            Math.Math.Abs(currentDelta.X) > 0.001f || Math.Math.Abs(currentDelta.Y) > 0.001f;
    }
}