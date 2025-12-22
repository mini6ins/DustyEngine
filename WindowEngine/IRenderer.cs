using GraphicsEngine;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace WindowEngine;

public interface IRenderer
{
    public void RunMainLoop(Action updateCallback, Vector2i resolution, string programTitle, string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode, string projectPath);
    GraphicsRenderer? Renderer { get; }
}
