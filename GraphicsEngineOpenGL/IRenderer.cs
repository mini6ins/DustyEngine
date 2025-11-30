using DustyEngine.Components;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GraphicsEngineOpenGL;

public interface IRenderer
{
    public void RunMainLoop(DustyEngine.Scene.Scene scene, Action updateCallback, Vector2i resolution, string programName, string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode);
    public void AddRenderer(MeshRenderer meshRenderer);
    public bool RemoveRenderer(int objectId);
}