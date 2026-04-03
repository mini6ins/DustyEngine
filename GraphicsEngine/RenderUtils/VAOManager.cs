using OpenTK.Graphics.OpenGL;

namespace GraphicsEngine.RenderUtils;

public sealed class VAOManager(ShaderProgram shaderProgram) : IDisposable
{
    private readonly ShaderProgram _shaderProgram = shaderProgram ?? throw new ArgumentNullException(nameof(shaderProgram));
    
    private readonly List<int> _vaoIds = [];
    private readonly List<int> _vboIds = [];
    private readonly List<int> _eboIds = [];
    private readonly List<int> _indexCounts = [];
    
    private bool _disposed;

    public void CreateVAO(float[] vertices, uint[] indices)
    {
        int vaoId = GL.GenVertexArray();
        GL.BindVertexArray(vaoId);

        int vbo = CreateVertexBuffer(vertices);
        int ebo = CreateIndexBuffer(indices);

        const int stride = 7 * sizeof(float); // position(3) + color(4)
        SetupVertexAttributes(stride);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        _vaoIds.Add(vaoId);
        _indexCounts.Add(indices.Length);
        _vboIds.Add(vbo);
        _eboIds.Add(ebo);
    }

    private int CreateVertexBuffer(float[] data)
    {
        int buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsage.StaticDraw);
        return buffer;
    }

    private int CreateIndexBuffer(uint[] data)
    {
        int buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, data.Length * sizeof(uint), data, BufferUsage.StaticDraw);
        return buffer;
    }

    private void SetupVertexAttributes(int stride)
    {
        SetupVertexAttribPointer("aPosition", 3, 0, stride);
        SetupVertexAttribPointer("aColor", 4, 3 * sizeof(float), stride);
    }

    private void SetupVertexAttribPointer(string attribName, int size, int offset, int stride)
    {
        int location = _shaderProgram.GetAttribLocation(attribName);
        GL.EnableVertexAttribArray((uint)location);
        GL.VertexAttribPointer((uint)location, size, VertexAttribPointerType.Float, false, stride, offset);
    }

    public void RenderVAO(int index)
    {
        if (index < 0 || index >= _vaoIds.Count) return;
        GL.BindVertexArray(_vaoIds[index]);
        GL.DrawElements(PrimitiveType.Triangles, _indexCounts[index], DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void DeleteVAOs()
    {
        foreach (var vao in _vaoIds) GL.DeleteVertexArray(vao);
        foreach (var vbo in _vboIds) GL.DeleteBuffer(vbo);
        foreach (var ebo in _eboIds) GL.DeleteBuffer(ebo);

        _vaoIds.Clear();
        _vboIds.Clear();
        _eboIds.Clear();
        _indexCounts.Clear();
    }

    public void DeleteVAO(int index)
    {
        if (index < 0 || index >= _vaoIds.Count)
        {
            Console.WriteLine($"Index {index} is out of bounds for VAOs.");
            return;
        }

        GL.DeleteVertexArray(_vaoIds[index]);
        GL.DeleteBuffer(_vboIds[index]);
        GL.DeleteBuffer(_eboIds[index]);

        _vaoIds.RemoveAt(index);
        _vboIds.RemoveAt(index);
        _eboIds.RemoveAt(index);
        _indexCounts.RemoveAt(index);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            DeleteVAOs();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~VAOManager()
    {
        Dispose(false);
    }
}
