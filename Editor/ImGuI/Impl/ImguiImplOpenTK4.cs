using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SNVector2 = System.Numerics.Vector2;

namespace Editor.ImGuI.Impl
{
    public static unsafe class ImguiImplOpenTK4
    {
        struct BackendData
        {
            public nint Context;
            public nint WindowPtr;

            public long Time;

            public Vector2 LastValidMousePos;

            public bool WantUpdateMonitors;
        }

        private class WindowCallbacks(NativeWindow window)
        {
            public void Window_MouseButton(MouseButtonEventArgs e)
            {
                var io = ImGui.GetIO();

                UpdateKeyModifiers(io, window);

                var button = (int)e.Button;
                if (button is >= 0 and <= (int)ImGuiMouseButton.COUNT)
                {
                    io.AddMouseButtonEvent((int)e.Button, e.IsPressed);
                }
            }

            public static void Window_MouseWheel(MouseWheelEventArgs e)
            {
                var io = ImGui.GetIO();
                io.AddMouseWheelEvent(e.OffsetX, e.OffsetY);
            }

            public void Window_KeyUp(KeyboardKeyEventArgs e) => Window_Key(e, false);
            public void Window_KeyDown(KeyboardKeyEventArgs e) => Window_Key(e, true);

            public void Window_Key(KeyboardKeyEventArgs e, bool isPressed)
            {
                var io = ImGui.GetIO();

                UpdateKeyModifiers(io, window);

                ImGuiKey imguiKey = TranslateKey(e.Key);
                io.AddKeyEvent(imguiKey, isPressed);
                io.SetKeyEventNativeData(imguiKey, (int)e.Key, e.ScanCode);
            }

            public static void Window_FocusedChanged(FocusedChangedEventArgs e)
            {
                var io = ImGui.GetIO();
                io.AddFocusEvent(e.IsFocused);
            }

            public void Window_MouseMove(MouseMoveEventArgs e)
            {
                var io = ImGui.GetIO();
                BackendData* bd = GetBackendData();

                var x = e.X;
                var y = e.Y;

                if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
                {
                    var clientLocation = window.ClientLocation;
                    x += clientLocation.X;
                    y += clientLocation.Y;
                }

                io.AddMousePosEvent(x, y);
                bd->LastValidMousePos = new Vector2(x, y);
            }

            public static void Window_MouseEnter()
            {
                var io = ImGui.GetIO();
                var bd = GetBackendData();

                io.AddMousePosEvent(bd->LastValidMousePos.X, bd->LastValidMousePos.Y);
            }

            public static void Window_MouseLeave()
            {
                var io = ImGui.GetIO();
                var bd = GetBackendData();

                bd->LastValidMousePos = new Vector2(io.MousePos.X, io.MousePos.Y);
                io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
            }

            public static void Window_TextInput(TextInputEventArgs e)
            {
                var io = ImGui.GetIO();

                io.AddInputCharacter((uint)e.Unicode);
            }
        }

        private static readonly Dictionary<nint, NativeWindow> WindowMap = new();
        private static readonly Dictionary<NativeWindow, WindowCallbacks> CallbackMap = new();

        private static BackendData* GetBackendData()
            => ImGui.GetCurrentContext() == 0 ? null : (BackendData*)ImGui.GetIO().BackendPlatformUserData;

        public static ImGuiKey TranslateKey(Keys key)
        {
            switch (key)
            {
                case >= Keys.D0 and <= Keys.D9:
                    return key - Keys.D0 + ImGuiKey._0;
                case >= Keys.A and <= Keys.Z:
                    return key - Keys.A + ImGuiKey.A;
                case >= Keys.KeyPad0 and <= Keys.KeyPad9:
                    return key - Keys.KeyPad0 + ImGuiKey.Keypad0;
                case >= Keys.F1 and <= Keys.F24:
                    return key - Keys.F1 + ImGuiKey.F24;
                default:
                    return key switch
                    {
                        Keys.Tab => ImGuiKey.Tab,
                        Keys.Left => ImGuiKey.LeftArrow,
                        Keys.Right => ImGuiKey.RightArrow,
                        Keys.Up => ImGuiKey.UpArrow,
                        Keys.Down => ImGuiKey.DownArrow,
                        Keys.PageUp => ImGuiKey.PageUp,
                        Keys.PageDown => ImGuiKey.PageDown,
                        Keys.Home => ImGuiKey.Home,
                        Keys.End => ImGuiKey.End,
                        Keys.Insert => ImGuiKey.Insert,
                        Keys.Delete => ImGuiKey.Delete,
                        Keys.Backspace => ImGuiKey.Backspace,
                        Keys.Space => ImGuiKey.Space,
                        Keys.Enter => ImGuiKey.Enter,
                        Keys.Escape => ImGuiKey.Escape,
                        Keys.Apostrophe => ImGuiKey.Apostrophe,
                        Keys.Comma => ImGuiKey.Comma,
                        Keys.Minus => ImGuiKey.Minus,
                        Keys.Period => ImGuiKey.Period,
                        Keys.Slash => ImGuiKey.Slash,
                        Keys.Semicolon => ImGuiKey.Semicolon,
                        Keys.Equal => ImGuiKey.Equal,
                        Keys.LeftBracket => ImGuiKey.LeftBracket,
                        Keys.Backslash => ImGuiKey.Backslash,
                        Keys.RightBracket => ImGuiKey.RightBracket,
                        Keys.GraveAccent => ImGuiKey.GraveAccent,
                        Keys.CapsLock => ImGuiKey.CapsLock,
                        Keys.ScrollLock => ImGuiKey.ScrollLock,
                        Keys.NumLock => ImGuiKey.NumLock,
                        Keys.PrintScreen => ImGuiKey.PrintScreen,
                        Keys.Pause => ImGuiKey.Pause,
                        Keys.KeyPadDecimal => ImGuiKey.KeypadDecimal,
                        Keys.KeyPadDivide => ImGuiKey.KeypadDivide,
                        Keys.KeyPadMultiply => ImGuiKey.KeypadMultiply,
                        Keys.KeyPadSubtract => ImGuiKey.KeypadSubtract,
                        Keys.KeyPadAdd => ImGuiKey.KeypadAdd,
                        Keys.KeyPadEnter => ImGuiKey.KeypadEnter,
                        Keys.KeyPadEqual => ImGuiKey.KeypadEqual,
                        Keys.LeftShift => ImGuiKey.LeftShift,
                        Keys.LeftControl => ImGuiKey.LeftCtrl,
                        Keys.LeftAlt => ImGuiKey.LeftAlt,
                        Keys.LeftSuper => ImGuiKey.LeftSuper,
                        Keys.RightShift => ImGuiKey.RightShift,
                        Keys.RightControl => ImGuiKey.RightCtrl,
                        Keys.RightAlt => ImGuiKey.RightAlt,
                        Keys.RightSuper => ImGuiKey.RightSuper,
                        Keys.Menu => ImGuiKey.Menu,
                        _ => ImGuiKey.None
                    };
            }
        }

        private static void UpdateKeyModifiers(ImGuiIOPtr io, NativeWindow window)
        {
            io.AddKeyEvent(ImGuiKey.ModCtrl, window.KeyboardState.IsKeyDown(Keys.LeftControl) || window.KeyboardState.IsKeyDown(Keys.RightControl));
            io.AddKeyEvent(ImGuiKey.ModShift, window.KeyboardState.IsKeyDown(Keys.LeftShift) || window.KeyboardState.IsKeyDown(Keys.RightShift));
            io.AddKeyEvent(ImGuiKey.ModAlt, window.KeyboardState.IsKeyDown(Keys.LeftAlt) || window.KeyboardState.IsKeyDown(Keys.RightAlt));
            io.AddKeyEvent(ImGuiKey.ModSuper, window.KeyboardState.IsKeyDown(Keys.LeftSuper) || window.KeyboardState.IsKeyDown(Keys.RightSuper));
        }

        private static void Monitors_OnMonitorConnected(MonitorEventArgs e)
        {
            var io = ImGui.GetIO();
            var bd = GetBackendData();

            bd->WantUpdateMonitors = true;
        }

        private static void InstallCallbacks(NativeWindow window)
        {
            var callbacks = new WindowCallbacks(window);

            window.MouseDown += callbacks.Window_MouseButton;
            window.MouseUp += callbacks.Window_MouseButton;
            window.MouseWheel += WindowCallbacks.Window_MouseWheel;
            window.KeyUp += callbacks.Window_KeyUp;
            window.KeyDown += callbacks.Window_KeyDown;
            window.FocusedChanged += WindowCallbacks.Window_FocusedChanged;
            window.MouseMove += callbacks.Window_MouseMove;
            window.MouseEnter += WindowCallbacks.Window_MouseEnter;
            window.MouseLeave += WindowCallbacks.Window_MouseLeave;
            window.TextInput += WindowCallbacks.Window_TextInput;

            CallbackMap.Add(window, callbacks);
        }

        private static void RestoreCallbacks(NativeWindow window)
        {
            var callbacks = CallbackMap[window];

            window.MouseDown -= callbacks.Window_MouseButton;
            window.MouseUp -= callbacks.Window_MouseButton;
            window.MouseWheel -= WindowCallbacks.Window_MouseWheel;
            window.KeyUp -= callbacks.Window_KeyUp;
            window.KeyDown -= callbacks.Window_KeyDown;
            window.FocusedChanged -= WindowCallbacks.Window_FocusedChanged;
            window.MouseMove -= callbacks.Window_MouseMove;
            window.MouseEnter -= WindowCallbacks.Window_MouseEnter;
            window.MouseLeave -= WindowCallbacks.Window_MouseLeave;
            window.TextInput -= WindowCallbacks.Window_TextInput;

            CallbackMap.Remove(window);
        }

        public static bool Init(NativeWindow window)
        {
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
            io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
            io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
            io.BackendFlags |= ImGuiBackendFlags.HasMouseHoveredViewport;

            var bd = (BackendData*)NativeMemory.AllocZeroed((uint)sizeof(BackendData));
            io.BackendPlatformUserData = (nint)bd;
            io.NativePtr->BackendPlatformName =
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("opentk_impl_opentk4"u8));
            WindowMap.Add((nint)window.WindowPtr, window);

            bd->Context = ImGui.GetCurrentContext();
            bd->WindowPtr = (nint)window.WindowPtr;
            bd->WantUpdateMonitors = true;

            var platformIO = ImGui.GetPlatformIO();
            platformIO.NativePtr->Platform_SetClipboardTextFn =
                (nint)(delegate* unmanaged[Cdecl]<nint, byte*, void>)(&Platform_SetClipboardText);
            platformIO.NativePtr->Platform_GetClipboardTextFn =
                (nint)(delegate* unmanaged[Cdecl]<nint, byte*>)(&Platform_GetClipboardText);

            platformIO.NativePtr->Monitors = default;

            InstallCallbacks(window);

            UpdateMonitors();
            Monitors.OnMonitorConnected += Monitors_OnMonitorConnected;

            var mainViewport = ImGui.GetMainViewport();
            mainViewport.PlatformHandle = (nint)window.WindowPtr;

            InitMultiViewportSupport();

            return true;
        }

        public static void Shutdown()
        {
            var bd = GetBackendData();
            var io = ImGui.GetIO();

            ShutdownMultiViewportSupport();

            io.NativePtr->BackendPlatformName = null;
            io.BackendPlatformUserData = 0;
            io.BackendFlags &= ~(ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.HasGamepad);
            if (WindowMap.TryGetValue(bd->WindowPtr, out var window))
                RestoreCallbacks(window);

            WindowMap.Remove(bd->WindowPtr);

            Monitors.OnMonitorConnected -= Monitors_OnMonitorConnected;

            NativeMemory.Free(bd);
        }

        private static void UpdateMouseData()
        {
            var io = ImGui.GetIO();
            var platformIO = ImGui.GetPlatformIO();

            uint mouse_viewport_id = 0;
            Vector2 prevMousePos = new(io.MousePos.X, io.MousePos.Y);

            for (var n = 0; n < platformIO.Viewports.Size; n++)
            {
                var viewport = platformIO.Viewports[n];
                var windowPtr = viewport.PlatformHandle;
                // FIXME:
                if (windowPtr == 0)
                    continue;
                var window = WindowMap[windowPtr];

                if (window.IsFocused)
                {
                    if (io.WantSetMousePos)
                        window.MousePosition = new Vector2(prevMousePos.X - viewport.Pos.X, prevMousePos.Y - viewport.Pos.Y);
                }

                var noInput = (viewport.Flags & ImGuiViewportFlags.NoInputs) != 0;
                window.MousePassthrough = noInput;

                if (GLFW.GetWindowAttrib(window.WindowPtr, WindowAttributeGetBool.Hovered))
                    mouse_viewport_id = viewport.ID;
            }

            if ((io.BackendFlags & ImGuiBackendFlags.HasMouseHoveredViewport) != 0)
                io.AddMouseViewportEvent(mouse_viewport_id);
        }

        private static void UpdateMouseCursor()
        {
            var io = ImGui.GetIO();
            var platformIO = ImGui.GetPlatformIO();
            var bd = GetBackendData();

            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0 ||
                WindowMap[bd->WindowPtr].CursorState == CursorState.Grabbed)
                return;

            var imguiCursor = ImGui.GetMouseCursor();
            for (int n = 0; n < platformIO.Viewports.Size; n++)
            {
                if (platformIO.Viewports[n].PlatformHandle == 0)
                    continue;

                var window = WindowMap[platformIO.Viewports[n].PlatformHandle];
                if (imguiCursor == ImGuiMouseCursor.None || io.MouseDrawCursor)
                {
                    window.CursorState = CursorState.Hidden;
                }
                else
                {
                    window.Cursor = GetCursor(imguiCursor);
                    window.CursorState = CursorState.Normal;
                }
            }

            return;

            static MouseCursor GetCursor(ImGuiMouseCursor imguiCursor)
            {
                return imguiCursor switch
                {
                    ImGuiMouseCursor.None => MouseCursor.Empty,
                    ImGuiMouseCursor.Arrow => MouseCursor.Default,
                    ImGuiMouseCursor.TextInput => MouseCursor.IBeam,
                    ImGuiMouseCursor.ResizeAll => MouseCursor.ResizeAll,
                    ImGuiMouseCursor.ResizeNS => MouseCursor.ResizeNS,
                    ImGuiMouseCursor.ResizeEW => MouseCursor.ResizeEW,
                    ImGuiMouseCursor.ResizeNESW => MouseCursor.ResizeNESW,
                    ImGuiMouseCursor.ResizeNWSE => MouseCursor.ResizeNWSE,
                    ImGuiMouseCursor.Hand => MouseCursor.PointingHand,
                    ImGuiMouseCursor.NotAllowed => MouseCursor.NotAllowed,
                    _ => MouseCursor.Default
                };
            }
        }

        private static void UpdateMonitors()
        {
            var io = ImGui.GetIO();
            var platformIO = ImGui.GetPlatformIO();
            var bd = GetBackendData();

            bd->WantUpdateMonitors = false;

            List<MonitorInfo> monitors = Monitors.GetMonitors();
            if (monitors.Count == 0)
                return;

            if (platformIO.NativePtr->Monitors.Data != 0)
                Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
            platformIO.NativePtr->Monitors = new ImVector(monitors.Count, monitors.Count,
                Marshal.AllocHGlobal(monitors.Count * sizeof(ImGuiPlatformMonitor)));
            NativeMemory.Clear((void*)platformIO.NativePtr->Monitors.Data,
                (nuint)(platformIO.NativePtr->Monitors.Capacity * sizeof(ImGuiPlatformMonitor)));
            for (var i = 0; i < monitors.Count; i++)
            {
                ref ImGuiPlatformMonitor monitor =
                    ref Unsafe.Add(ref Unsafe.AsRef<ImGuiPlatformMonitor>((void*)platformIO.Monitors.Data), i);

                var clientArea = monitors[i].ClientArea;
                monitor.MainPos = new SNVector2(clientArea.Min.X, clientArea.Min.Y);
                monitor.MainSize = new SNVector2(clientArea.Size.X, clientArea.Size.Y);

                var workArea = monitors[i].WorkArea;
                monitor.WorkPos = new SNVector2(workArea.Min.X, workArea.Min.Y);
                monitor.WorkSize = new SNVector2(workArea.Size.X, workArea.Size.Y);
                monitor.DpiScale = monitors[i].HorizontalScale;
                monitor.PlatformHandle = (void*)monitors[i].Handle.Pointer;
            }
        }

        public static void NewFrame()
        {
            var io = ImGui.GetIO();
            var bd = GetBackendData();

            var window = WindowMap[bd->WindowPtr];
            Vector2 clientSize = window.ClientSize;
            Vector2 fbSize = window.FramebufferSize;
            io.DisplaySize = new SNVector2(fbSize.X, fbSize.Y);
            if (fbSize is { X: > 0, Y: > 0 })
                io.DisplayFramebufferScale = new SNVector2(clientSize.X / fbSize.X, clientSize.Y / fbSize.Y);

            if (bd->WantUpdateMonitors) UpdateMonitors();

            var currentTime = Stopwatch.GetTimestamp();
            if (currentTime <= bd->Time)
                currentTime = bd->Time + 1;

            io.DeltaTime = bd->Time > 0.0 ? (currentTime - bd->Time) / (float)Stopwatch.Frequency : 1.0f / 60.0f;
            bd->Time = currentTime;

            UpdateMouseData();
            UpdateMouseCursor();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_SetClipboardText(nint ctx, byte* text)
        {
            var bd = GetBackendData();
            GLFW.SetClipboardString((Window*)bd->WindowPtr, Marshal.PtrToStringUTF8((nint)text));
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static byte* Platform_GetClipboardText(nint ctx)
        {
            var bd = GetBackendData();
            return GLFW.GetClipboardStringRaw((Window*)bd->WindowPtr);
        }

        private struct ViewportData
        {
            public nint WindowPtr;
            public bool WindowOwned;
            public int IgnoreWindowPosEventFrame;
            public int IgnoreWindowSizeEventFrame;
        }

        private static void InitMultiViewportSupport()
        {
            var platformIO = ImGui.GetPlatformIO();
            var bd = GetBackendData();

            platformIO.Platform_CreateWindow = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&Platform_CreateWindow;
            platformIO.Platform_DestroyWindow = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&Platform_DestroyWindow;
            platformIO.Platform_ShowWindow = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&Platform_ShowWindow;
            ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(platformIO, (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, SNVector2*, void>)&Platform_GetWindowPos);
            platformIO.Platform_SetWindowPos = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, SNVector2, void>)&Platform_SetWindowPos;
            ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(platformIO, (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, SNVector2*, void>)&Platform_GetWindowSize);
            platformIO.Platform_SetWindowSize = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, SNVector2, void>)&Platform_SetWindowSize;
            platformIO.Platform_SetWindowTitle = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, nint, void>)&Platform_SetWindowTitle;
            platformIO.Platform_SetWindowFocus = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&Platform_SetWindowFocus;
            platformIO.Platform_GetWindowFocus = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, byte>)&Platform_GetWindowFocus;
            platformIO.Platform_GetWindowMinimized = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, byte>)&Platform_GetWindowMinimized;
            platformIO.Platform_SetWindowAlpha = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, float, void>)&Platform_SetWindowAlpha;
            platformIO.Platform_RenderWindow = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void*, void>)&Platform_RenderWindow;
            platformIO.Platform_SwapBuffers = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void*, void>)&Platform_SwapBuffers;

            var mainViewport = ImGui.GetMainViewport();
            var vd = (ViewportData*)NativeMemory.AllocZeroed((uint)sizeof(ViewportData));
            vd->WindowPtr = bd->WindowPtr;
            vd->WindowOwned = false;
            mainViewport.PlatformUserData = (nint)vd;
            mainViewport.PlatformHandle = bd->WindowPtr;
        }

        private static void ShutdownMultiViewportSupport() => ImGui.DestroyPlatformWindows();

        private static void Window_Resize(ResizeEventArgs e)
        {
            // FIXME: Get the platform handle...?
        }

        private static void Window_Move(WindowPositionEventArgs obj)
        {
            // FIXME: Get the platform handle...?
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_CreateWindow(ImGuiViewportPtr viewport)
        {
            var bd = GetBackendData();
            var mainWindow = WindowMap[bd->WindowPtr];

            var vd = (ViewportData*)NativeMemory.AllocZeroed((uint)sizeof(ViewportData));
            viewport.PlatformUserData = (nint)vd;

            // FIXME??
            GLFW.WindowHint(WindowHintBool.FocusOnShow, false);
            GLFW.WindowHint(WindowHintBool.Floating, viewport.Flags.HasFlag(ImGuiViewportFlags.TopMost));
            var window = new NativeWindow(new NativeWindowSettings()
            {
                StartVisible = false,
                StartFocused = false,
                WindowBorder = viewport.Flags.HasFlag(ImGuiViewportFlags.NoDecoration)
                    ? WindowBorder.Hidden
                    : WindowBorder.Resizable,
                SharedContext = mainWindow.Context,
                Title = "No Title Yet",
            });
            WindowMap.Add((nint)window.WindowPtr, window);

            vd->WindowPtr = (nint)window.WindowPtr;
            vd->WindowOwned = true;
            viewport.PlatformHandle = vd->WindowPtr;

            window.ClientLocation = new Vector2i((int)viewport.Pos.X, (int)viewport.Pos.Y);

            InstallCallbacks(window);
            window.Move += Window_Move;
            window.Resize += Window_Resize;

            if (window.API != ContextAPI.OpenGL) return;

            window.MakeCurrent();
            window.VSync = VSyncMode.Off;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_DestroyWindow(ImGuiViewportPtr viewport)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            if (vd != null)
            {
                if (vd->WindowOwned)
                {
                    var window = WindowMap[vd->WindowPtr];
                    window.Dispose();

                    WindowMap.Remove(vd->WindowPtr);
                }

                vd->WindowPtr = 0;
                NativeMemory.Free(vd);
            }

            viewport.PlatformUserData = viewport.PlatformHandle = 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_ShowWindow(ImGuiViewportPtr viewport)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            var window = WindowMap[vd->WindowPtr];
            window.IsVisible = true;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_GetWindowPos(ImGuiViewportPtr viewport, SNVector2* outPos)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            if (WindowMap.TryGetValue(vd->WindowPtr, out var window))
            {
                *outPos = new SNVector2(window.ClientLocation.X, window.ClientLocation.Y);
            }
            else
            {
                *outPos = default;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_SetWindowPos(ImGuiViewportPtr viewport, SNVector2 pos)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            var window = WindowMap[vd->WindowPtr];
            window.ClientLocation = new Vector2i((int)pos.X, (int)pos.Y);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_GetWindowSize(ImGuiViewportPtr viewport, SNVector2* outSize)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            var window = WindowMap[vd->WindowPtr];
            *outSize = new SNVector2(window.ClientSize.X, window.ClientSize.Y);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_SetWindowSize(ImGuiViewportPtr viewport, SNVector2 size)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            var window = WindowMap[vd->WindowPtr];
            window.ClientSize = new Vector2i((int)size.X, (int)size.Y);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        public static void Platform_SetWindowTitle(ImGuiViewportPtr viewport, nint name)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            var window = WindowMap[vd->WindowPtr];
            window.Title = Marshal.PtrToStringUTF8(name);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        public static void Platform_SetWindowFocus(ImGuiViewportPtr viewport)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            var window = WindowMap[vd->WindowPtr];
            window.Focus();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static byte Platform_GetWindowFocus(ImGuiViewportPtr viewport)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            using NativeWindow window = WindowMap[vd->WindowPtr];
            return window.IsFocused ? (byte)1 : (byte)0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static byte Platform_GetWindowMinimized(ImGuiViewportPtr viewport)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            using NativeWindow window = WindowMap[vd->WindowPtr];
            return window.WindowState == WindowState.Minimized ? (byte)1 : (byte)0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_SetWindowAlpha(ImGuiViewportPtr viewport, float alpha)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            using var window = WindowMap[vd->WindowPtr];
            GLFW.SetWindowOpacity(window.WindowPtr, alpha);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_RenderWindow(ImGuiViewportPtr viewport, void* _)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            using var window = WindowMap[vd->WindowPtr];
            if (window.API == ContextAPI.OpenGL) window.MakeCurrent();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Platform_SwapBuffers(ImGuiViewportPtr viewport, void* _)
        {
            var vd = (ViewportData*)viewport.PlatformUserData;
            var window = WindowMap[vd->WindowPtr];
            if (window.API != ContextAPI.OpenGL) return;

            window.MakeCurrent();
            window.Context.SwapBuffers();
        }
    }
}
