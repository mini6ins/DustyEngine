using System.Numerics;
using ImGuiNET;

namespace DustyEngineEditor.Panels.ViewPortPanel;

internal class ViewPortPanel(InputHandler inputHandler) : IRenderablePanel
{
    private int _textureWidth;
    private int _textureHeight;
    private int _texture;
    private int _framesDisplayed;

    public bool IsRemoteWindowFocused { get; private set; }

    public Action? OnStartClicked;
    public Action? OnStopClicked;

    public void UpdateData(int texture, int textureWidth, int textureHeight, int framesDisplayed)
    {
        _texture = texture;
        _textureWidth = textureWidth;
        _textureHeight = textureHeight;
        _framesDisplayed = framesDisplayed;
    }

    public int GetFramesDisplayed() => _framesDisplayed;

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 170), ImGuiCond.FirstUseEver);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);

        ImGui.Begin("Renderer Viewport", ImGuiWindowFlags.NoCollapse);
        
        if (ImGui.Button("Start"))
            OnStartClicked?.Invoke();
        

        ImGui.SameLine();

        if (ImGui.Button("Stop"))
            OnStopClicked?.Invoke();
        

        ImGui.Separator();

        IsRemoteWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        var availableSize = ImGui.GetContentRegionAvail();
        if (availableSize is { X: > 32, Y: > 32 })
        {
            var imageSize = CalculateImageSize(availableSize, _textureWidth, _textureHeight);
            var cursorScreenPos = CenterImage(availableSize, imageSize);

            ImGui.Image(new IntPtr(_texture), imageSize, new Vector2(0, 1), new Vector2(1, 0));
            _framesDisplayed++;

            if (IsRemoteWindowFocused && ImGui.IsItemHovered())
            {
                inputHandler.ProcessMouse(imageSize, cursorScreenPos);
            }
        }

        if (IsRemoteWindowFocused)
        {
            inputHandler.ProcessKeyboard();
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private static Vector2 CalculateImageSize(Vector2 availableSize, int textureWidth, int textureHeight)
    {
        var targetAspectRatio = (float)textureWidth / Math.Max(1, textureHeight);
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
}