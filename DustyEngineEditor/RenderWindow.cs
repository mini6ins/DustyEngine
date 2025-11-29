using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGui_OpenTK.Backends;

public class RenderWindow : GameWindow
{
    private IRemoteRenderer _remoteRenderer;
    private int _texture;
    private float _time = 0f;
    private FrameData?[] _frameBuffer = new FrameData?[3];
    private volatile int _readyBufferIndex = 0;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private Task? _fetcherTask;
    private int _framesReceived = 0;
    private int _framesDisplayed = 0;
    private DateTime _lastStatsTime = DateTime.Now;
    private float _lastReceivedTimestamp = -1f;
    private readonly HashSet<ImGuiKey> _pressedKeys = new HashSet<ImGuiKey>();
    private readonly HashSet<ImGuiMouseButton> _pressedMouseButtons = new HashSet<ImGuiMouseButton>();
    private System.Numerics.Vector2 _lastMousePos = System.Numerics.Vector2.Zero;
    private bool _showHelp = true;
    private bool _isRemoteWindowFocused = false;
    private int _currentTextureWidth = 0;
    private int _currentTextureHeight = 0;

    public RenderWindow(IRemoteRenderer remoteRenderer)
        : base(GameWindowSettings.Default,
            new NativeWindowSettings()
            {
                Size = new Vector2i(1024, 768),
                Title = "Remote Renderer Client",
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                Flags = ContextFlags.Default
            })
    {
        _remoteRenderer = remoteRenderer;
        UpdateFrequency = 200;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();
        ImguiImplOpenTK4.Init(this);
        ImguiImplOpenGL3.Init();
        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _texture);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        byte[] px = new byte[] { 32, 32, 32, 255 };
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, 1, 1, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, px);
        _currentTextureWidth = 1;
        _currentTextureHeight = 1;
        _fetcherTask = Task.Run(FetchFramesLoop);
        Console.WriteLine("Client ready. WASD to move camera, mouse to rotate.");
    }

    private async Task FetchFramesLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var frame = await _remoteRenderer.GetFrameData(_time);
                
                if (frame?.PixelData?.Length > 0)
                {
                    // ✅ Всегда обновляем кадр
                    int writeIndex = 1 - _readyBufferIndex;
                    _frameBuffer[writeIndex] = frame;
                    _readyBufferIndex = writeIndex;
                    _lastReceivedTimestamp = frame.Timestamp;
                    _framesReceived++;
                }
                
                // ✅ Минимальная задержка для предотвращения 100% CPU
                await Task.Delay(1, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching frame: {ex.Message}");
                await Task.Delay(16, _cts.Token);
            }
        }
    }

    
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        _time += (float)args.Time;
        
        // ✅ Берем последний полученный кадр
        var frameToUpdate = _frameBuffer[_readyBufferIndex];
        if (frameToUpdate?.PixelData?.Length > 0)
        {
            UpdateTexture(frameToUpdate);
        }
        
        var now = DateTime.Now;
        if ((now - _lastStatsTime).TotalSeconds >= 1.0)
        {
            string helpText = _showHelp ? " | H:hide" : " | H:help";
            Title = $"Remote Renderer - Recv: {_framesReceived} | Display: {_framesDisplayed} FPS{helpText}";
            _framesReceived = 0;
            _framesDisplayed = 0;
            _lastStatsTime = now;
        }
        
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
        
        if (_isRemoteWindowFocused)
        {
            CheckFunctionKeysRaw();
        }
    }
    
    private readonly HashSet<Keys> _pressedRawKeys = new HashSet<Keys>();
    
    private void CheckFunctionKeysRaw()
    {
        var functionKeys = new[] 
        { 
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6,
            Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12
        };
        
        foreach (var key in functionKeys)
        {
            bool isDown = KeyboardState.IsKeyDown(key);
            bool wasPressed = _pressedRawKeys.Contains(key);
            
            if (isDown && !wasPressed)
            {
                _pressedRawKeys.Add(key);
                string keyName = key.ToString().ToUpper();
                
            
                
           
                Task.Run(() =>
                {
                    try { _remoteRenderer.OnKeyDown(keyName); }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine($"[CLIENT RAW] Error: {ex.Message}");
                    }
                });
            }
            else if (!isDown && wasPressed)
            {
                _pressedRawKeys.Remove(key);
                string keyName = key.ToString().ToUpper();
                
                Task.Run(() =>
                {
                    try { _remoteRenderer.OnKeyUp(keyName); }
                    catch { }
                });
            }
        }
    }

    private void UpdateTexture(FrameData frameData)
    {
        try
        {
            GL.BindTexture(TextureTarget.Texture2d, _texture);
            
            if (_currentTextureWidth != frameData.Width || _currentTextureHeight != frameData.Height)
            {
                _currentTextureWidth = frameData.Width;
                _currentTextureHeight = frameData.Height;
            }
            
            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba,
                frameData.Width, frameData.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, frameData.PixelData);
            
            GL.BindTexture(TextureTarget.Texture2d, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating texture: {ex.Message}");
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();
        RenderImGui();
        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
        SwapBuffers();
    }

    private void RenderImGui()
    {
        ImGui.DockSpaceOverViewport();
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Settings Panel"))
        {
            string helpText = _showHelp ? " | H:hide" : " | H:show";
            ImGui.Text($"Recv: {_framesReceived} | Display: {_framesDisplayed} FPS{helpText}");
            ImGui.Text($"Texture: {_currentTextureWidth} x {_currentTextureHeight}");
            ImGui.Separator();
            if (_showHelp)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.4f, 1f, 0.4f, 1f), "=== CONTROLS ===");
                ImGui.Text("WASD - Move | Mouse - Rotate Camera");
                ImGui.Text("Space - Up | Shift - Down");
                ImGui.Text("F1 - Wireframe | F2 - Fill");
                ImGui.Text("H - Toggle Help | F11 - Fullscreen");
                ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 0.4f, 1f), "Focus viewport to send input!");
            }
            if (ImGui.Button("Fullscreen (F11)"))
            {
                WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
            }
        }
        ImGui.End();
        RenderSceneViewport();
    }

    private void RenderSceneViewport()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 170), ImGuiCond.FirstUseEver);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        ImGui.Begin("Remote Renderer Viewport", ImGuiWindowFlags.NoCollapse);
        _isRemoteWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        var availableSize = ImGui.GetContentRegionAvail();
        if (availableSize.X > 32 && availableSize.Y > 32)
        {
            float targetAspectRatio = (float)_currentTextureWidth / Math.Max(1, _currentTextureHeight);
            float availableAspectRatio = availableSize.X / availableSize.Y;
            System.Numerics.Vector2 imageSize;
            if (availableAspectRatio > targetAspectRatio)
            {
                imageSize.Y = availableSize.Y;
                imageSize.X = imageSize.Y * targetAspectRatio;
            }
            else
            {
                imageSize.X = availableSize.X;
                imageSize.Y = imageSize.X / targetAspectRatio;
            }
            var cursor = ImGui.GetCursorPos();
            cursor.X += (availableSize.X - imageSize.X) * 0.5f;
            cursor.Y += (availableSize.Y - imageSize.Y) * 0.5f;
            ImGui.SetCursorPos(cursor);
            var cursorScreenPos = ImGui.GetCursorScreenPos();
            ImGui.Image(new IntPtr(_texture), imageSize, 
                new System.Numerics.Vector2(0, 1), 
                new System.Numerics.Vector2(1, 0));
            _framesDisplayed++;
            if (_isRemoteWindowFocused && ImGui.IsItemHovered())
            {
                ProcessInput(imageSize, cursorScreenPos);
            }
        }
        if (_isRemoteWindowFocused)
        {
            ProcessKeyboard();
        }
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void ProcessInput(System.Numerics.Vector2 imageSize, System.Numerics.Vector2 imagePos)
    {
        var mousePos = ImGui.GetMousePos();
    
        float deltaX = mousePos.X - _lastMousePos.X;
        float deltaY = mousePos.Y - _lastMousePos.Y;
    
        if (System.Math.Abs(deltaX) > 0.1f || System.Math.Abs(deltaY) > 0.1f)
        {
            _lastMousePos = mousePos;
            try { _remoteRenderer.OnMouseMoveDelta(deltaX, deltaY); }
            catch { }
        }
    
        float normalizedX = ((mousePos.X - imagePos.X) / imageSize.X) * 2.0f - 1.0f;
        float normalizedY = -(((mousePos.Y - imagePos.Y) / imageSize.Y) * 2.0f - 1.0f);
    
        var mouseButtons = new[]
        {
            (ImGuiMouseButton.Left, 0),
            (ImGuiMouseButton.Right, 1),
            (ImGuiMouseButton.Middle, 2)
        };
        foreach (var (button, buttonIndex) in mouseButtons) 
        {
            bool isDown = ImGui.IsMouseDown(button);
            bool wasPressed = _pressedMouseButtons.Contains(button);
            if (isDown && !wasPressed)
            {
                _pressedMouseButtons.Add(button);
                try { _remoteRenderer.OnMouseDown(normalizedX, normalizedY, buttonIndex); }
                catch { }
            }
            else if (!isDown && wasPressed)
            {
                _pressedMouseButtons.Remove(button);
                try { _remoteRenderer.OnMouseUp(normalizedX, normalizedY, buttonIndex); }
                catch { }
            }
        }
    }

    private void ProcessKeyboard()
    {
        var keysToCheck = new[]
        {
            ImGuiKey.A, ImGuiKey.B, ImGuiKey.C, ImGuiKey.D, ImGuiKey.E, ImGuiKey.F, ImGuiKey.G, ImGuiKey.H,
            ImGuiKey.I, ImGuiKey.J, ImGuiKey.K, ImGuiKey.L, ImGuiKey.M, ImGuiKey.N, ImGuiKey.O, ImGuiKey.P,
            ImGuiKey.Q, ImGuiKey.R, ImGuiKey.S, ImGuiKey.T, ImGuiKey.U, ImGuiKey.V, ImGuiKey.W, ImGuiKey.X,
            ImGuiKey.Y, ImGuiKey.Z,
            ImGuiKey._0, ImGuiKey._1, ImGuiKey._2, ImGuiKey._3, ImGuiKey._4,
            ImGuiKey._5, ImGuiKey._6, ImGuiKey._7, ImGuiKey._8, ImGuiKey._9,
            ImGuiKey.Space, ImGuiKey.Enter, ImGuiKey.Tab, ImGuiKey.Backspace, ImGuiKey.Delete,
            ImGuiKey.Insert, ImGuiKey.Home, ImGuiKey.End, ImGuiKey.PageUp, ImGuiKey.PageDown, ImGuiKey.Escape,
            ImGuiKey.UpArrow, ImGuiKey.DownArrow, ImGuiKey.LeftArrow, ImGuiKey.RightArrow,
            ImGuiKey.LeftShift, ImGuiKey.RightShift, ImGuiKey.LeftCtrl, ImGuiKey.RightCtrl,
            ImGuiKey.LeftAlt, ImGuiKey.RightAlt, ImGuiKey.LeftSuper, ImGuiKey.RightSuper,
            ImGuiKey.CapsLock, ImGuiKey.ScrollLock, ImGuiKey.NumLock, ImGuiKey.PrintScreen,
            ImGuiKey.Pause, ImGuiKey.Menu,
            ImGuiKey.Keypad0, ImGuiKey.Keypad1, ImGuiKey.Keypad2, ImGuiKey.Keypad3, ImGuiKey.Keypad4,
            ImGuiKey.Keypad5, ImGuiKey.Keypad6, ImGuiKey.Keypad7, ImGuiKey.Keypad8, ImGuiKey.Keypad9,
            ImGuiKey.KeypadDecimal, ImGuiKey.KeypadDivide, ImGuiKey.KeypadMultiply,
            ImGuiKey.KeypadSubtract, ImGuiKey.KeypadAdd, ImGuiKey.KeypadEnter, ImGuiKey.KeypadEqual
            // ✅ УБРАЛИ ВСЕ СИМВОЛЬНЫЕ КЛАВИШИ - они мапятся неправильно из-за бага ImGui с раскладкой
            // ImGuiKey.Apostrophe, ImGuiKey.Comma, ImGuiKey.Minus, ImGuiKey.Period, ImGuiKey.Slash,
            // ImGuiKey.Semicolon, ImGuiKey.Equal, ImGuiKey.LeftBracket, ImGuiKey.Backslash,
            // ImGuiKey.RightBracket, ImGuiKey.GraveAccent
        };

        foreach (var key in keysToCheck)
        {
            bool isDown = ImGui.IsKeyDown(key);
            bool wasPressed = _pressedKeys.Contains(key);
            if (isDown && !wasPressed)
            {
                _pressedKeys.Add(key);
                
                string keyName = GetKeyName(key);
                
                // Локальная обработка только для H
                if (key == ImGuiKey.H)
                {
                    _showHelp = !_showHelp;
                }
                
                // Отправка на сервер
                Task.Run(() =>
                {
                    try { _remoteRenderer.OnKeyDown(keyName); }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine($"[CLIENT] Error sending key: {ex.Message}");
                    }
                });
            }
            else if (!isDown && wasPressed)
            {
                _pressedKeys.Remove(key);
                string keyName = GetKeyName(key);
                Task.Run(() =>
                {
                    try { _remoteRenderer.OnKeyUp(keyName); }
                    catch { }
                });
            }
        }
    }

    private string GetKeyName(ImGuiKey key)
    {
        return key switch
        {
            // Буквы A-Z
            ImGuiKey.A => "A", ImGuiKey.B => "B", ImGuiKey.C => "C", ImGuiKey.D => "D",
            ImGuiKey.E => "E", ImGuiKey.F => "F", ImGuiKey.G => "G", ImGuiKey.H => "H",
            ImGuiKey.I => "I", ImGuiKey.J => "J", ImGuiKey.K => "K", ImGuiKey.L => "L",
            ImGuiKey.M => "M", ImGuiKey.N => "N", ImGuiKey.O => "O", ImGuiKey.P => "P",
            ImGuiKey.Q => "Q", ImGuiKey.R => "R", ImGuiKey.S => "S", ImGuiKey.T => "T",
            ImGuiKey.U => "U", ImGuiKey.V => "V", ImGuiKey.W => "W", ImGuiKey.X => "X",
            ImGuiKey.Y => "Y", ImGuiKey.Z => "Z",
            
            // Цифры 0-9
            ImGuiKey._0 => "0", ImGuiKey._1 => "1", ImGuiKey._2 => "2", ImGuiKey._3 => "3",
            ImGuiKey._4 => "4", ImGuiKey._5 => "5", ImGuiKey._6 => "6", ImGuiKey._7 => "7",
            ImGuiKey._8 => "8", ImGuiKey._9 => "9",
            
            // Специальные клавиши
            ImGuiKey.Space => "SPACE",
            ImGuiKey.Enter => "ENTER",
            ImGuiKey.Tab => "TAB",
            ImGuiKey.Backspace => "BACKSPACE",
            ImGuiKey.Delete => "DELETE",
            ImGuiKey.Insert => "INSERT",
            ImGuiKey.Home => "HOME",
            ImGuiKey.End => "END",
            ImGuiKey.PageUp => "PAGEUP",
            ImGuiKey.PageDown => "PAGEDOWN",
            ImGuiKey.Escape => "ESCAPE",
            
            // Стрелки
            ImGuiKey.UpArrow => "UP",
            ImGuiKey.DownArrow => "DOWN",
            ImGuiKey.LeftArrow => "LEFT",
            ImGuiKey.RightArrow => "RIGHT",
            
            // Функциональные клавиши
            ImGuiKey.F1 => "F1", ImGuiKey.F2 => "F2", ImGuiKey.F3 => "F3", ImGuiKey.F4 => "F4",
            ImGuiKey.F5 => "F5", ImGuiKey.F6 => "F6", ImGuiKey.F7 => "F7", ImGuiKey.F8 => "F8",
            ImGuiKey.F9 => "F9", ImGuiKey.F10 => "F10", ImGuiKey.F11 => "F11", ImGuiKey.F12 => "F12",
            
            // Модификаторы
            ImGuiKey.LeftShift => "LEFTSHIFT",
            ImGuiKey.RightShift => "RIGHTSHIFT",
            ImGuiKey.LeftCtrl => "LEFTCONTROL",
            ImGuiKey.RightCtrl => "RIGHTCONTROL",
            ImGuiKey.LeftAlt => "LEFTALT",
            ImGuiKey.RightAlt => "RIGHTALT",
            ImGuiKey.LeftSuper => "LEFTSUPER",
            ImGuiKey.RightSuper => "RIGHTSUPER",
            
            // Lock клавиши
            ImGuiKey.CapsLock => "CAPSLOCK",
            ImGuiKey.ScrollLock => "SCROLLLOCK",
            ImGuiKey.NumLock => "NUMLOCK",
            ImGuiKey.PrintScreen => "PRINTSCREEN",
            ImGuiKey.Pause => "PAUSE",
            ImGuiKey.Menu => "MENU",
            
            // Numpad
            ImGuiKey.Keypad0 => "KP0",
            ImGuiKey.Keypad1 => "KP1",
            ImGuiKey.Keypad2 => "KP2",
            ImGuiKey.Keypad3 => "KP3",
            ImGuiKey.Keypad4 => "KP4",
            ImGuiKey.Keypad5 => "KP5",
            ImGuiKey.Keypad6 => "KP6",
            ImGuiKey.Keypad7 => "KP7",
            ImGuiKey.Keypad8 => "KP8",
            ImGuiKey.Keypad9 => "KP9",
            ImGuiKey.KeypadDecimal => "KPDECIMAL",
            ImGuiKey.KeypadDivide => "KPDIVIDE",
            ImGuiKey.KeypadMultiply => "KPMULTIPLY",
            ImGuiKey.KeypadSubtract => "KPSUBTRACT",
            ImGuiKey.KeypadAdd => "KPADD",
            ImGuiKey.KeypadEnter => "KPENTER",
            ImGuiKey.KeypadEqual => "KPEQUAL",
            
            // Символы (это была проблема!)
            ImGuiKey.Apostrophe => "APOSTROPHE",      // '
            ImGuiKey.Comma => "COMMA",                // ,
            ImGuiKey.Minus => "MINUS",                // -
            ImGuiKey.Period => "PERIOD",              // .
            ImGuiKey.Slash => "SLASH",                // /
            ImGuiKey.Semicolon => "SEMICOLON",        // ;
            ImGuiKey.Equal => "EQUAL",                // =
            ImGuiKey.LeftBracket => "LEFTBRACKET",    // [
            ImGuiKey.Backslash => "BACKSLASH",        // \
            ImGuiKey.RightBracket => "RIGHTBRACKET",  // ]
            ImGuiKey.GraveAccent => "GRAVEACCENT",    // `
            
            _ => key.ToString().ToUpper()
        };
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        _cts.Cancel();
        _fetcherTask?.Wait(TimeSpan.FromSeconds(2));
        _cts.Dispose();
        GL.DeleteTexture(_texture);
        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        ImGui.DestroyContext();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }
}