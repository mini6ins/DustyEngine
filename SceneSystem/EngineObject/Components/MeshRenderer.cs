using System.Text.Json.Serialization;
using DustyEngine.Scene;
using SceneSystem.Attributes;
using Utils;

namespace DustyEngine.Components;

public class MeshRenderer : MonoBehaviour
{
    public int a = 123;

    public string? Path
    {
        get => _path;
        set
        {
            if (_path == value) return;
            _path = value;
            Mesh = null;
            if (!string.IsNullOrEmpty(_path))
                LoadMeshFromPath();
        }
    }

    private string? _path;
    public Mesh Mesh;
    private bool _isRegistered;

    public MeshRenderer()
    {
    }

    public MeshRenderer(Mesh? mesh = null, string? path = null)
    {
        if (mesh != null)
        {
            Mesh = mesh;
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
        if (Mesh == null && !string.IsNullOrEmpty(Path))
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
                Mesh = new Mesh(vertices, indices);
                Debug.Log(
                    $"MeshRenderer: Successfully loaded mesh from '{Path}' with {vertices.Length} vertices and {indices.Length} indices.",
                    Debug.LogLevel.Info, true);

                RegisterRenderer();
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

    private void RegisterRenderer()
    {
        if (!_isRegistered && Mesh != null)
        {
            SceneManager.AddRenderer(this);
            _isRegistered = true;
        }
    }

    private void UnregisterRenderer()
    {
        if (!_isRegistered) return;
        SceneManager.RemoveRenderer(this);
        _isRegistered = false;
    }

    public void SetMesh(Mesh? mesh)
    {
        Mesh = mesh;
        if (mesh != null)
        {
            Debug.Log(
                $"MeshRenderer: Mesh set directly with {mesh.Vertices.Length} vertices and {mesh.Indices.Length} indices.",
                Debug.LogLevel.Info, true);
            RegisterRenderer();
        }
        else
        {
            Debug.Log("MeshRenderer: Mesh set to null.", Debug.LogLevel.Info, true);
            UnregisterRenderer();
        }
    }


    private void OnEnable()
    {
        if (Mesh == null && !string.IsNullOrEmpty(Path)) LoadMeshFromPath();

        else if (Mesh != null) RegisterRenderer();
    }

    private void OnDisable() => UnregisterRenderer();
    public int GetVertexCount() => Mesh?.Vertices?.Length ?? 0;
    public int GetIndexCount() => Mesh?.Indices?.Length ?? 0;

    public override string ToString() =>
        $"MeshRenderer [Vertices: {GetVertexCount()}, Indices: {GetIndexCount()}] (Path: {Path ?? "Direct"})";
}

public class Mesh : Component
{
    [JsonIgnore] public float[] Vertices { get; }
    [JsonIgnore] public uint[] Indices { get; }

    [JsonIgnore] public int TriangleCount => Indices.Length / 3;

   public bool IsValid() => Vertices.Length > 0 && Indices.Length > 0;


    public Mesh(float[] vertices, uint[] indices)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices ?? throw new ArgumentNullException(nameof(indices));
    }


    public override string ToString()
    {
        return $"Mesh [Vertices: {Vertices.Length}, Indices: {Indices.Length}, Triangles: {TriangleCount}]";
    }
}
