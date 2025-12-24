using DustyEngine.Components;
using SceneSystem.EngineObject.GameObject;

namespace DustyEngine.Scene;

public class Scene
{
    public string Name { get; set; }
    public List<GameObject> GameObjects { get; set; } = [];
    public List<Component> Components { get; set; } = [];
    public string Path { get; set; } = string.Empty;
}
