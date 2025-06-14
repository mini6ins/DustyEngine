using DustyEngine.Components;

namespace DustyEngine.Scene;

public class Scene
{
    public string Name { get; set; }
    public List<GameObject> GameObjects { get; set; } = [];
    public List<Component> Components { get; set; } = [];
}
