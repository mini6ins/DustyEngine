
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
        private static OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState _keyboardState;

        public static void Update(OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState keyboardState)
        {
            _keyboardState = keyboardState;
        }

        public static bool IsKeyDown(KeyCode keyCode)
        {
            return _keyboardState.IsKeyDown(ConvertKey(keyCode));
        }

        public static bool IsKeyPressed(KeyCode keyCode)
        {
            return _keyboardState.IsKeyPressed(ConvertKey(keyCode));
        }

        public static bool IsKeyReleased(KeyCode keyCode)
        {
            return _keyboardState.IsKeyReleased(ConvertKey(keyCode));
        }

        private static OpenTK.Windowing.GraphicsLibraryFramework.Keys ConvertKey(KeyCode keyCode)
        {
            return Enum.TryParse(keyCode.ToString(), out OpenTK.Windowing.GraphicsLibraryFramework.Keys result)
                ? result
                : OpenTK.Windowing.GraphicsLibraryFramework.Keys.Unknown;
        }

        // --- Mouse Input ---
        private static bool _firstMove = true;
        private static float _lastX;
        private static float _lastY;
        public static (float X, float Y) Delta { get; private set; } = (0, 0);

        public static void UpdateMouse(float x, float y)
        {
            if (_firstMove)
            {
                _lastX = x;
                _lastY = y;
                _firstMove = false;
                Delta = (0, 0);
                return;
            }

            Delta = (x - _lastX, y - _lastY);
            _lastX = x;
            _lastY = y;
        }

        public static void ResetMouse()
        {
            _firstMove = true;
        }
    }
}
