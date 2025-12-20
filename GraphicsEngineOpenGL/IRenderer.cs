using DustyEngine.Components;
using GraphicsEngineOpenGL;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GraphicsEngine;

public interface IRenderer
{
    public void RunMainLoop(Action updateCallback, Vector2i resolution, string programTitle, string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode, string projectPath);
    public void AddRenderer(MeshRenderer meshRenderer);
    public bool RemoveRenderer(int objectId);
}
