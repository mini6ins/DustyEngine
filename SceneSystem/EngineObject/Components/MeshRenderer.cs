using Utils;

namespace DustyEngine.Components;

public class MeshRenderer : MonoBehaviour
{
    public string? Path
    {
        get => _path;
        set
        {
            if (_path == value) return;
            _path = value;
            _mesh = null;
            if (!string.IsNullOrEmpty(_path))
                LoadMeshFromPath();
        }
    }

    private string? _path;
    private Mesh? _mesh;

    public MeshRenderer()
    {
    }

    public MeshRenderer(Mesh? mesh = null, string? path = null)
    {
        if (mesh != null)
        {
            _mesh = mesh;
            Debug.Log(
                $"MeshRenderer: Mesh provided directly with {mesh.Vertices.Length} vertices and {mesh.Indices.Length} indices.",
                Debug.LogLevel.Info, true);
            return;
        }

        Path = path;

        if (!string.IsNullOrEmpty(Path)) return;
        Debug.Log("MeshRenderer: No mesh or path provided. MeshRenderer will be empty.", Debug.LogLevel.Warning);
    }

    public void EnsureLoaded()
    {
        if (_mesh == null && !string.IsNullOrEmpty(Path))
            LoadMeshFromPath();
    }

    private void LoadMeshFromPath()
    {
        if (string.IsNullOrEmpty(Path))
        {
            Debug.Log("MeshRenderer: Path is null or empty. Cannot load mesh.", Debug.LogLevel.Warning, false);
            return;
        }

        try
        {
            if (OBJModelLoader.LoadModel(Path, out var vertices, out var indices))
            {
                _mesh = new Mesh(vertices, indices);
                Debug.Log(
                    $"MeshRenderer: Successfully loaded mesh from '{Path}' with {vertices.Length} vertices and {indices.Length} indices.",
                    Debug.LogLevel.Info, true);
            }
            else
            {
                Debug.Log($"MeshRenderer: Failed to load mesh from '{Path}'. OBJModelLoader returned false.",
                    Debug.LogLevel.Error, false);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"MeshRenderer: Exception while loading mesh from '{Path}': {ex.Message}", Debug.LogLevel.Error);
        }
    }

    public void SetMesh(Mesh? mesh)
    {
        _mesh = mesh;
        if (mesh != null)
        {
            Debug.Log(
                $"MeshRenderer: Mesh set directly with {mesh.Vertices.Length} vertices and {mesh.Indices.Length} indices.",
                Debug.LogLevel.Info, true);
        }
        else
        {
            Debug.Log("MeshRenderer: Mesh set to null.", Debug.LogLevel.Info, true);
        }
    }

    public Mesh? GetMesh() => _mesh;
    public int GetVertexCount() => _mesh?.Vertices?.Length ?? 0;
    public int GetIndexCount() => _mesh?.Indices?.Length ?? 0;


    private void OnEnable()
    {
        if (_mesh == null && !string.IsNullOrEmpty(Path))
            LoadMeshFromPath();
    }

    public override string ToString()
    {
        if (_mesh == null)
        {
            return $"MeshRenderer [No Mesh] (Path: {Path ?? "None"})";
        }

        return $"MeshRenderer [Vertices: {GetVertexCount()}, Indices: {GetIndexCount()}] (Path: {Path ?? "Direct"})";
    }
}

public class Mesh
{
    public float[] Vertices { get; }
    public uint[] Indices { get; }

    public Mesh(float[] vertices, uint[] indices)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices ?? throw new ArgumentNullException(nameof(indices));
    }

    public bool IsValid() => Vertices.Length > 0 && Indices.Length > 0;

    public int TriangleCount => Indices.Length / 3;

    public override string ToString()
    {
        return $"Mesh [Vertices: {Vertices.Length}, Indices: {Indices.Length}, Triangles: {TriangleCount}]";
    }
}
