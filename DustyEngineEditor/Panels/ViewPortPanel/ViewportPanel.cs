using System.Numerics;
using DustyEngine;
using GraphicsEngineOpenGL;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;

namespace DustyEngineEditor.Panels.ViewPortPanel;

internal class ViewportPanel : IRenderablePanel, IDisposable
{
    private readonly InputHandler _inputHandler;
    private int _texture;
    private int _textureWidth = 1;
    private int _textureHeight = 1;
    private float _time;

    private readonly FrameData?[] _frameBuffer = new FrameData?[3];
    private volatile int _readyBufferIndex;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _fetcherTask;

    public bool IsRemoteWindowFocused { get; private set; }

    public ViewportPanel(InputHandler inputHandler)
    {
        _inputHandler = inputHandler;
        InitializeTexture();
        _fetcherTask = Task.Run(FetchFramesLoop);
    }

    private void InitializeTexture()
    {
        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _texture);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        byte[] px = [32, 32, 32, 255];
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, 1, 1, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, px);

        GL.BindTexture(TextureTarget.Texture2d, 0);
    }

    private async Task FetchFramesLoop()
    {
        const int errorDelayMs = 16;
        const int normalDelayMs = 1;

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var frame = await Editor.RemoteRenderer!.GetFrameData(_time);

                if (frame?.PixelData?.Length > 0)
                {
                    var writeIndex = 1 - _readyBufferIndex;
                    _frameBuffer[writeIndex] = frame;
                    _readyBufferIndex = writeIndex;
                }

                await Task.Delay(normalDelayMs, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ConsolePanel.ConsolePanel.Log($"Error fetching frame: {ex.Message}");
                await Task.Delay(errorDelayMs, _cts.Token);
            }
        }
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;

        var frameToUpdate = _frameBuffer[_readyBufferIndex];
        if (frameToUpdate?.PixelData?.Length > 0)
            UpdateTexture(frameToUpdate);

        if (IsRemoteWindowFocused)
            _inputHandler.ProcessKeyboard();
    }

    private void UpdateTexture(FrameData frameData)
    {
        try
        {
            GL.BindTexture(TextureTarget.Texture2d, _texture);

            if (_textureWidth != frameData.Width || _textureHeight != frameData.Height)
            {
                _textureWidth = frameData.Width;
                _textureHeight = frameData.Height;

                Debug.Log($"Texture resized to {_textureWidth}x{_textureHeight}");
            }

            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba,
                frameData.Width, frameData.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, frameData.PixelData);

            GL.BindTexture(TextureTarget.Texture2d, 0);
        }
        catch (Exception ex)
        {
            ConsolePanel.ConsolePanel.Log($"Error updating texture: {ex.Message}");
        }
    }

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 170), ImGuiCond.FirstUseEver);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);

        ImGui.Begin("Renderer Viewport", ImGuiWindowFlags.NoCollapse);

        RenderControls();
        ImGui.Separator();

        IsRemoteWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        RenderViewport();

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private static void RenderControls()
    {
        if (ImGui.Button("Start"))
            Editor.RemoteRenderer!.PlayEngine();

        ImGui.SameLine();

        if (ImGui.Button("Stop"))
            Editor.RemoteRenderer!.StopEngine();
    }

    private void RenderViewport()
    {
        var availableSize = ImGui.GetContentRegionAvail();

        if (availableSize.X <= 32 || availableSize.Y <= 32)
            return;

        var imageSize = CalculateImageSize(availableSize, _textureWidth, _textureHeight);
        var cursorScreenPos = CenterImage(availableSize, imageSize);

        ImGui.Image(new IntPtr(_texture), imageSize, new Vector2(0, 1), new Vector2(1, 0));

        if (IsRemoteWindowFocused && ImGui.IsItemHovered())
            _inputHandler.ProcessMouse(imageSize, cursorScreenPos);
    }

    private static Vector2 CalculateImageSize(Vector2 availableSize, int textureWidth, int textureHeight)
    {
        var targetAspectRatio = (float)textureWidth / System.Math.Max(1, textureHeight);
        var availableAspectRatio = availableSize.X / availableSize.Y;

        Vector2 imageSize;

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

        return imageSize;
    }

    private static Vector2 CenterImage(Vector2 availableSize, Vector2 imageSize)
    {
        var cursor = ImGui.GetCursorPos();
        cursor.X += (availableSize.X - imageSize.X) * 0.5f;
        cursor.Y += (availableSize.Y - imageSize.Y) * 0.5f;
        ImGui.SetCursorPos(cursor);
        return ImGui.GetCursorScreenPos();
    }

    public void Dispose()
    {
        _cts.Cancel();

        try
        {
            _fetcherTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex)
        {
            foreach (var inner in ex.InnerExceptions)
            {
                if (inner is not OperationCanceledException)
                    ConsolePanel.ConsolePanel.Log($"Error during dispose: {inner.Message}");
            }
        }

        _cts.Dispose();

        if (_texture == 0) return;

        GL.DeleteTexture(_texture);
        _texture = 0;
    }
}
