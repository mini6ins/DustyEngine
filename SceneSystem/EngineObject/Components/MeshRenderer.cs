using System.Text.Json.Serialization;
using DustyEngine.Core;
using DustyEngine.Scene;
using Utils;

namespace DustyEngine.Components;

public class MeshRenderer : MonoBehaviour
{
    public string? ObjPath
    {
        get => _objPath;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : PathUtility.GetRelativePath(value);

            if (_objPath == normalized) return;

            _objPath = normalized;
            _mesh = null;

            if (!string.IsNullOrEmpty(_objPath))
                LoadMeshFromPath();
        }
    }

    private string? _objPath;

    [JsonIgnore] private Mesh? _mesh;
    [JsonIgnore] private bool _isRegistered;

    public MeshRenderer()
    {
    }

    public MeshRenderer(Mesh? mesh = null, string? objPath = null)
    {
        if (mesh != null)
        {
            _mesh = mesh;
            return;
        }

        ObjPath = objPath;
    }

    public void EnsureLoaded()
    {
        if (_mesh == null && !string.IsNullOrEmpty(_objPath))
            LoadMeshFromPath();
    }

    private void LoadMeshFromPath()
    {
        if (string.IsNullOrEmpty(_objPath))
        {
            Debug.Log("MeshRenderer: ObjPath is null or empty. Cannot load mesh.", Debug.LogLevel.Warning, false);
            return;
        }

        var absPath = PathUtility.GetAbsolutePath(_objPath);

        try
        {
            if (OBJModelLoader.LoadModel(absPath, out var vertices, out var indices))
            {
                _mesh = new Mesh(vertices, indices);
                Debug.Log(
                    $"MeshRenderer: Loaded mesh from '{_objPath}' (abs: '{absPath}') V={vertices.Length} I={indices.Length}.",
                    Debug.LogLevel.Info, true);

                RegisterRenderer();
            }
            else
            {
                Debug.Log($"MeshRenderer: Failed to load mesh from '{absPath}'.", Debug.LogLevel.Error, false);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"MeshRenderer: Exception while loading mesh from '{absPath}': {ex.Message}",
                Debug.LogLevel.Error);
        }
    }

    private void RegisterRenderer()
    {
        if (_isRegistered || _mesh == null) return;
        SceneManager.AddRenderer(this);
        _isRegistered = true;
    }

    private void UnregisterRenderer()
    {
        if (!_isRegistered) return;
        SceneManager.RemoveRenderer(this);
        _isRegistered = false;
    }

    public Mesh? GetMesh() => _mesh;

    public void SetMesh(Mesh? mesh)
    {
        _mesh = mesh;

        if (_mesh != null) RegisterRenderer();
        else UnregisterRenderer();
    }

    private void OnEnable()
    {
        EnsureLoaded();
        if (_mesh != null) RegisterRenderer();
    }

    private void OnDisable() => UnregisterRenderer();

    public int GetVertexCount() => _mesh?.Vertices?.Length ?? 0;
    public int GetIndexCount() => _mesh?.Indices?.Length ?? 0;

    public override string ToString() => $"MeshRenderer [V:{GetVertexCount()}, I:{GetIndexCount()}] (ObjPath: {_objPath ?? "Direct"})";
}

public class Mesh(float[] vertices, uint[] indices) : Component
{
    [JsonIgnore] public float[] Vertices { get; } = vertices ?? throw new ArgumentNullException(nameof(vertices));
    [JsonIgnore] public uint[] Indices { get; } = indices ?? throw new ArgumentNullException(nameof(indices));

    [JsonIgnore] public int TriangleCount => Indices.Length / 3;

    public override string ToString()
    {
        return $"Mesh [Vertices: {Vertices.Length}, Indices: {Indices.Length}, Triangles: {TriangleCount}]";
    }
}
