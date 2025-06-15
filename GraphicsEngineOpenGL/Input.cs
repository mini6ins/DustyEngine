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
        public static void Update(OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState newKeyboardState)
        {
            previousKeyboardState = keyboardState;
            keyboardState = newKeyboardState;
        }

        public static bool IsKeyDown(KeyCode keyCode)
        {
            if (keyboardState == null) return false;
            return keyboardState.IsKeyDown(ConvertKey(keyCode));
        }

        public static bool IsKeyPressed(KeyCode keyCode)
        {
            if (keyboardState == null || previousKeyboardState == null) return false;
            var key = ConvertKey(keyCode);
            return keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            if (keyboardState == null || previousKeyboardState == null) return false;
            var key = ConvertKey(keyCode);
            return !keyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyDown(key);
        }
        
        public static bool IsKeyJustActivatedOnce(KeyCode keyCode)
        {
            if (keyboardState == null) return false;

            var key = ConvertKey(keyCode);
    
            if (keyboardState.IsKeyDown(key))
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

 
        
        private static bool firstMove = true;
        private static float lastX;
        private static float lastY;
        private static (float X, float Y) currentDelta = (0, 0);

        public static (float X, float Y) Delta => currentDelta;

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
        
        public static void ResetMouse()
        {
            currentDelta = (0, 0);
        }
        
        public static void FullResetMouse()
        {
            firstMove = true;
            currentDelta = (0, 0);
        }
        
        public static bool HasMouseMoved()
        {
            return Math.Abs(currentDelta.X) > 0.001f || Math.Abs(currentDelta.Y) > 0.001f;
        }

        public static float GetMouseSensitivityAdjustedDelta(float sensitivity)
        {
            return Math.Sqrt(currentDelta.X * currentDelta.X + currentDelta.Y * currentDelta.Y) * sensitivity;
        }
    }
}