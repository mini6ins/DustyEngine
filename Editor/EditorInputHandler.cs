using InputSystem;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;

namespace Editor;

public static class EditorInputHandler
{
    private static float _lastMouseX;
    private static float _lastMouseY;
    private static bool _firstMouseMove = true;

    public static void UpdateMouseInput(MouseState mouseState)
    {
        var mousePos = mouseState.Position;

        if (_firstMouseMove)
        {
            _lastMouseX = mousePos.X;
            _lastMouseY = mousePos.Y;
            _firstMouseMove = false;
            return;
        }

        var deltaX = mousePos.X - _lastMouseX;
        var deltaY = mousePos.Y - _lastMouseY;

        if (System.Math.Abs(deltaX) > 0.001f || System.Math.Abs(deltaY) > 0.001f)
            Input.RpcMouseMove(deltaX, deltaY);

        _lastMouseX = mousePos.X;
        _lastMouseY = mousePos.Y;

        if (mouseState.IsButtonDown(MouseButton.Middle))
            Input.RpcMouseDown(InputSystem.MouseButton.Middle);
        else
            Input.RpcMouseUp(InputSystem.MouseButton.Middle);

        if (mouseState.IsButtonDown(MouseButton.Left))
            Input.RpcMouseDown(InputSystem.MouseButton.Left);
        else
            Input.RpcMouseUp(InputSystem.MouseButton.Left);

        if (mouseState.IsButtonDown(MouseButton.Right))
            Input.RpcMouseDown(InputSystem.MouseButton.Right);
        else
            Input.RpcMouseUp(InputSystem.MouseButton.Right);
    }

    public static void UpdateKeyboardInput(KeyboardState keyboardState)
    {
        var keys = new[]
        {
            (Keys.W, "W"),
            (Keys.A, "A"),
            (Keys.S, "S"),
            (Keys.D, "D"),
            (Keys.Space, "SPACE"),
            (Keys.LeftShift, "LEFTSHIFT"),
            (Keys.F1, "F1"),
            (Keys.F2, "F2"),
        };

        foreach (var (key, name) in keys)
        {
            if (keyboardState.IsKeyDown(key))
                Input.RpcKeyDown(name);
            else
                Input.RpcKeyUp(name);
        }
    }
}
