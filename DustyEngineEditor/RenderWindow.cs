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
    
    // Тройная буферизация для плавности
    private FrameData?[] _frameBuffer = new FrameData?[3];
    private volatile int _readyBufferIndex = 0;
    
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private Task? _fetcherTask;
    
    // Статистика
    private int _framesReceived = 0;
    private int _framesDisplayed = 0;
    private DateTime _lastStatsTime = DateTime.Now;
    private float _lastReceivedTimestamp = -1f;
    
    // Отслеживание всех клавиш
    private readonly HashSet<ImGuiKey> _pressedKeys = new HashSet<ImGuiKey>();
    private bool _showHelp = true;
    private bool _isRemoteWindowFocused = false;
    
    // Информация о текущей текстуре
    private int _currentTextureWidth = 0;
    private int _currentTextureHeight = 0;

    public RenderWindow(IRemoteRenderer remoteRenderer)
        : base(GameWindowSettings.Default,
            new NativeWindowSettings()
            {
                Size = new Vector2i(1024, 768),
                Title = "Remote Renderer Client - ImGui - 200 FPS",
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

        // Инициализация ImGui
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();
        
        ImguiImplOpenTK4.Init(this);
        ImguiImplOpenGL3.Init();

        // Создание текстуры для remote renderer
        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _texture);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        // Заглушка - 1x1 темный пиксель
        byte[] px = new byte[] { 32, 32, 32, 255 };
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, 1, 1, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, px);
        
        _currentTextureWidth = 1;
        _currentTextureHeight = 1;

        // Запуск агрессивного фетчера
        _fetcherTask = Task.Run(FetchFramesLoop);

        Console.WriteLine("Client window loaded. Ready to receive frames at 200 FPS.");
        Console.WriteLine("\n=== CONTROLS ===");
        Console.WriteLine("W/S - Scale | A/D - Rotate | Arrows - Move");
        Console.WriteLine("R/G/B - Color | Space - Reset | H - Help");
        Console.WriteLine("Mouse - Move | LMB - Random | RMB - Scale");
        Console.WriteLine("ALL KEYS are sent to server!");
        Console.WriteLine("================\n");
    }

    private async Task FetchFramesLoop()
    {
        // Максимально агрессивная стратегия для 200 FPS
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var frame = await _remoteRenderer.GetFrameData(_time);
                
                if (frame?.PixelData?.Length > 0)
                {
                    bool isNewFrame = Math.Abs(frame.Timestamp - _lastReceivedTimestamp) > 0.0001f;
                    
                    if (isNewFrame)
                    {
                        // Запись в следующий буфер
                        int writeIndex = (_readyBufferIndex + 1) % 3;
                        _frameBuffer[writeIndex] = frame;
                        _readyBufferIndex = writeIndex;
                        
                        _lastReceivedTimestamp = frame.Timestamp;
                        _framesReceived++;
                    }
                }
                
                // Минимальная задержка для снижения нагрузки на CPU
                await Task.Delay(1, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching frame: {ex.Message}");
                await Task.Delay(5, _cts.Token);
            }
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _time += (float)args.Time;

        // Обновление текстуры если есть новый кадр
        var frameToUpdate = _frameBuffer[_readyBufferIndex];
        if (frameToUpdate?.PixelData?.Length > 0)
        {
            UpdateTexture(frameToUpdate);
        }

        // Статистика каждую секунду
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

        // Начало ImGui frame
        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();

        // Render ImGui UI
        RenderImGui();

        // Завершение ImGui frame
        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

        SwapBuffers();
    }

    private void RenderImGui()
    {
        // Docking space на весь viewport
        ImGui.DockSpaceOverViewport();

        // Панель настроек/статистики
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("Settings Panel"))
        {
            string helpText = _showHelp ? " | H:hide help" : " | H:show help";
            ImGui.Text($"Recv: {_framesReceived} | Display: {_framesDisplayed} FPS{helpText}");
            ImGui.Text($"Texture: {_currentTextureWidth} x {_currentTextureHeight}");
            ImGui.Separator();
            
            if (_showHelp)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.4f, 1f, 0.4f, 1f), "=== CONTROLS ===");
                ImGui.Text("W/S - Scale | A/D - Rotate");
                ImGui.Text("Arrows - Move | R/G/B - Color");
                ImGui.Text("Space - Reset | H - Toggle Help");
                ImGui.Text("Mouse - Move | LMB/RMB - Actions");
                ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 0.4f, 1f), "Focus viewport to send input!");
            }
            
            if (ImGui.Button("Fullscreen (F11)"))
            {
                WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
            }
        }
        ImGui.End();

        // Main viewport с remote renderer текстурой
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
            // Вычисляем размер с сохранением aspect ratio
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
            
            // Центрируем изображение
            var cursor = ImGui.GetCursorPos();
            cursor.X += (availableSize.X - imageSize.X) * 0.5f;
            cursor.Y += (availableSize.Y - imageSize.Y) * 0.5f;
            ImGui.SetCursorPos(cursor);
            
            var cursorScreenPos = ImGui.GetCursorScreenPos();
            
            // Отображаем текстуру (перевернуто по Y для OpenGL)
            ImGui.Image(new IntPtr(_texture), imageSize, 
                new System.Numerics.Vector2(0, 1), 
                new System.Numerics.Vector2(1, 0));
            
            _framesDisplayed++;
            
            // Обработка ввода если окно в фокусе
            if (_isRemoteWindowFocused && ImGui.IsItemHovered())
            {
                ProcessInput(imageSize, cursorScreenPos);
            }
        }
        
        // Обработка клавиатуры (если окно в фокусе)
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
        
        // Нормализованные координаты (как в оригинале)
        float normalizedX = ((mousePos.X - imagePos.X) / imageSize.X) * 2.0f - 1.0f;
        float normalizedY = -(((mousePos.Y - imagePos.Y) / imageSize.Y) * 2.0f - 1.0f);

        // Mouse move
        Task.Run(() =>
        {
            try { _remoteRenderer.OnMouseMove(normalizedX, normalizedY); }
            catch { }
        });

        // Mouse clicks
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            Task.Run(() =>
            {
                try { _remoteRenderer.OnMouseClick(normalizedX, normalizedY, 0); }
                catch { }
            });
        }
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            Task.Run(() =>
            {
                try { _remoteRenderer.OnMouseClick(normalizedX, normalizedY, 1); }
                catch { }
            });
        }
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
        {
            Task.Run(() =>
            {
                try { _remoteRenderer.OnMouseClick(normalizedX, normalizedY, 2); }
                catch { }
            });
        }
    }

    private void ProcessKeyboard()
    {
        // Все клавиши из оригинала
        var keysToCheck = new[]
        {
            ImGuiKey.W, ImGuiKey.A, ImGuiKey.S, ImGuiKey.D,
            ImGuiKey.R, ImGuiKey.G, ImGuiKey.B, ImGuiKey.Space, ImGuiKey.H,
            ImGuiKey.UpArrow, ImGuiKey.DownArrow, ImGuiKey.LeftArrow, ImGuiKey.RightArrow,
            ImGuiKey.Enter, ImGuiKey.Tab, ImGuiKey.Backspace, ImGuiKey.Delete,
            ImGuiKey.Insert, ImGuiKey.Home, ImGuiKey.End, ImGuiKey.PageUp, ImGuiKey.PageDown,
            ImGuiKey.F1, ImGuiKey.F2, ImGuiKey.F3, ImGuiKey.F4, ImGuiKey.F5, ImGuiKey.F6,
            ImGuiKey.F7, ImGuiKey.F8, ImGuiKey.F9, ImGuiKey.F10, ImGuiKey.F11, ImGuiKey.F12
        };

        foreach (var key in keysToCheck)
        {
            bool isDown = ImGui.IsKeyDown(key);
            bool wasPressed = _pressedKeys.Contains(key);

            if (isDown && !wasPressed)
            {
                _pressedKeys.Add(key);
                
                // Toggle help (как в оригинале)
                if (key == ImGuiKey.H)
                {
                    _showHelp = !_showHelp;
                }
                
                // Fullscreen toggle
                if (key == ImGuiKey.F11)
                {
                    WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
                }
                
                // Отправка на сервер (как в оригинале)
                string keyName = GetKeyName(key);
                Task.Run(() =>
                {
                    try { _remoteRenderer.OnKeyPress(keyName); }
                    catch { }
                });
            }
            else if (!isDown && wasPressed)
            {
                _pressedKeys.Remove(key);
            }
        }
    }

    private string GetKeyName(ImGuiKey key)
    {
        // Преобразование ImGuiKey в формат сервера (точно как в оригинале)
        return key switch
        {
            ImGuiKey.Space => "SPACE",
            ImGuiKey.UpArrow => "UP",
            ImGuiKey.DownArrow => "DOWN",
            ImGuiKey.LeftArrow => "LEFT",
            ImGuiKey.RightArrow => "RIGHT",
            ImGuiKey.Enter => "ENTER",
            ImGuiKey.Tab => "TAB",
            ImGuiKey.Backspace => "BACKSPACE",
            ImGuiKey.Delete => "DELETE",
            ImGuiKey.Insert => "INSERT",
            ImGuiKey.Home => "HOME",
            ImGuiKey.End => "END",
            ImGuiKey.PageUp => "PAGEUP",
            ImGuiKey.PageDown => "PAGEDOWN",
            ImGuiKey.CapsLock => "CAPSLOCK",
            ImGuiKey.PrintScreen => "PRINTSCREEN",
            ImGuiKey.Pause => "PAUSE",
            ImGuiKey.F1 => "F1",
            ImGuiKey.F2 => "F2",
            ImGuiKey.F3 => "F3",
            ImGuiKey.F4 => "F4",
            ImGuiKey.F5 => "F5",
            ImGuiKey.F6 => "F6",
            ImGuiKey.F7 => "F7",
            ImGuiKey.F8 => "F8",
            ImGuiKey.F9 => "F9",
            ImGuiKey.F10 => "F10",
            ImGuiKey.F11 => "F11",
            ImGuiKey.F12 => "F12",
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