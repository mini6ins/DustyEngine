using Utils;

namespace DustyEngine.Components;

public class MeshRenderer : MonoBehaviour
{
    public string Path { get; set; }
    private Mesh _mesh;

    private void OnEnable()
    {
        if (!OBJModelLoader.LoadModel(Path, out var vertices, out var indices)) return;
        _mesh = new Mesh(vertices, indices);
        Debug.Log(
            $"MeshRenderer: Successfully loaded mesh from '{Path}' with {vertices.Length} vertices and {indices.Length} indices.",
            Debug.LogLevel.Info, true);
    }

    public Mesh? GetMesh() => _mesh;
}

public class Mesh(float[] vertices, uint[] indices)
{
    public float[] Vertices { get; } = vertices;
    public uint[] Indices { get; } = indices;
}