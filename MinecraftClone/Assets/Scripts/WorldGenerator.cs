using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using SceneSystem.EngineObject.GameObject;

public class WorldGenerator : MonoBehaviour
{
    private readonly float[] _vertices =
    [
        // position xyz + color rgba
        // Front (+Z)
        -0.5f, -0.5f,  0.5f,  1f, 0f, 0f, 1f,
        0.5f, -0.5f,  0.5f,  1f, 0f, 0f, 1f,
        0.5f,  0.5f,  0.5f,  1f, 0f, 0f, 1f,
        -0.5f,  0.5f,  0.5f,  1f, 0f, 0f, 1f,

        // Back (-Z)
        0.5f, -0.5f, -0.5f,  0f, 1f, 0f, 1f,
        -0.5f, -0.5f, -0.5f,  0f, 1f, 0f, 1f,
        -0.5f,  0.5f, -0.5f,  0f, 1f, 0f, 1f,
        0.5f,  0.5f, -0.5f,  0f, 1f, 0f, 1f,

        // Left (-X)
        -0.5f, -0.5f, -0.5f,  0f, 0f, 1f, 1f,
        -0.5f, -0.5f,  0.5f,  0f, 0f, 1f, 1f,
        -0.5f,  0.5f,  0.5f,  0f, 0f, 1f, 1f,
        -0.5f,  0.5f, -0.5f,  0f, 0f, 1f, 1f,

        // Right (+X)
        0.5f, -0.5f,  0.5f,  1f, 1f, 0f, 1f,
        0.5f, -0.5f, -0.5f,  1f, 1f, 0f, 1f,
        0.5f,  0.5f, -0.5f,  1f, 1f, 0f, 1f,
        0.5f,  0.5f,  0.5f,  1f, 1f, 0f, 1f,

        // Top (+Y)
        -0.5f,  0.5f,  0.5f,  0f, 1f, 1f, 1f,
        0.5f,  0.5f,  0.5f,  0f, 1f, 1f, 1f,
        0.5f,  0.5f, -0.5f,  0f, 1f, 1f, 1f,
        -0.5f,  0.5f, -0.5f,  0f, 1f, 1f, 1f,

        // Bottom (-Y)
        -0.5f, -0.5f, -0.5f,  1f, 0f, 1f, 1f,
        0.5f, -0.5f, -0.5f,  1f, 0f, 1f, 1f,
        0.5f, -0.5f,  0.5f,  1f, 0f, 1f, 1f,
        -0.5f, -0.5f,  0.5f,  1f, 0f, 1f, 1f,
    ];

    private readonly uint[] _indices =
    [
        0,  1,  2,  0,  2,  3,
        4,  5,  6,  4,  6,  7,
        8,  9, 10,  8, 10, 11,
        12, 13, 14, 12, 14, 15,
        16, 17, 18, 16, 18, 19,
        20, 21, 22, 20, 22, 23,
    ];

    
    
    private void Start()
    {
        var cube = new GameObject("Cube");
        cube.AddComponent(new MeshRenderer(new Mesh(_vertices, _indices)));
        Instantiate(cube, GameObject, new Vector3(0,0,0), new Quaternion(), new Vector3(1,1,1));
    }
}