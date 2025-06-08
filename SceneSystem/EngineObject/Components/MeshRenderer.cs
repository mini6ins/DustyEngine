using Utils;

namespace DustyEngine.Components;

public class MeshRenderer : MonoBehaviour
{
    public string Path { get; set; }
    private Mesh _mesh;

    public void OnEnable()
    {
        if (OBJModelLoader.LoadModel(Path, out float[] vertices, out uint[] indices))
        {
            _mesh = new Mesh(vertices, indices);
            Debug.Log(
                $"MeshRenderer: Successfully loaded mesh from '{Path}' with {vertices.Length} vertices and {indices.Length} indices.",
                Debug.LogLevel.Info, true);
        }
    }

    public Mesh? GetMesh() => _mesh;
}

public class Mesh
{
    public float[] Vertices { get; }
    public uint[] Indices { get; }

    public Mesh(float[] vertices, uint[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}