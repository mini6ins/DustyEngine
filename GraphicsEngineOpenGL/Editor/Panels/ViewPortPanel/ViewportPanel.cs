using System.Numerics;
using GraphicsEngineOpenGL;
using ImGuiNET;
using IRenderablePanel = GraphicsEngineOpenGL.Editor.IRenderablePanel;

namespace DustyEngineEditor.Panels.ViewPortPanel;

internal class ViewportPanel : IRenderablePanel
{
    public void Render()
    {
        ImGui.Begin("Scene Viewport");

        var size = ImGui.GetContentRegionAvail();
        if (size.X > 0 && size.Y > 0)
        {
            GraphicsEngineOpenGl.Renderer?.ResizeViewport((int)size.X, (int)size.Y);

            if (GraphicsEngineOpenGl.Renderer != null)
                ImGui.Image(
                    GraphicsEngineOpenGl.Renderer.ViewportTexture,
                    size,
                    new Vector2(0, 1),
                    new Vector2(1, 0));
        }

        ImGui.End();
    }

}
