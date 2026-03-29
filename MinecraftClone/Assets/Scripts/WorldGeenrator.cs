using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using SceneSystem.EngineObject.GameObject;

public class WorldGeenrator : MonoBehaviour
{
    public string CubePath;

    private readonly float[] _vertices =
    [
        -0.5f, -0.5f, -0.5f, // 0
        0.5f, -0.5f, -0.5f, // 1
        0.5f,  0.5f, -0.5f, // 2
        -0.5f,  0.5f, -0.5f, // 3

        -0.5f, -0.5f,  0.5f, // 4
        0.5f, -0.5f,  0.5f, // 5
        0.5f,  0.5f,  0.5f, // 6
        -0.5f,  0.5f,  0.5f  // 7
    ];

    private readonly uint[] _indices =
    [
        0, 2, 1, 0, 3, 2,
        4, 5, 6, 4, 6, 7,
        0, 7, 3, 0, 4, 7,
        1, 2, 6, 1, 6, 5,
        3, 7, 6, 3, 6, 2,
        0, 1, 5, 0, 5, 4
    ];

    
    
    private void Start()
    {
        var cube = new GameObject("Cube");
        cube.AddComponent(new MeshRenderer(new Mesh(_vertices, _indices)));
        Instantiate(cube, GameObject, new Vector3(0,0,0), new Quaternion(), new Vector3(1,1,1));
    }
}