using OpenTK.Graphics.OpenGL;

namespace GraphicsEngineOpenGL;

public class VAOManager : IDisposable
{
    private ShaderProgram shaderProgram;
    private List<int> vaoIds = new();
    private List<int> indexCounts = new();
    private List<int> vboIds = new();
    private List<int> eboIds = new();
    private bool disposed = false;

    public VAOManager(ShaderProgram shaderProgram)
    {
        this.shaderProgram = shaderProgram ?? throw new ArgumentNullException(nameof(shaderProgram));
    }

    public void CreateVAO(float[] vertices, uint[] indices)
    {
        int vaoId = GL.GenVertexArray();
        GL.BindVertexArray(vaoId);

        int vbo = CreateVertexBuffer(vertices);
        int ebo = CreateIndexBuffer(indices);

        const int STRIDE = 7 * sizeof(float); // position(3) + color(4)
        SetupVertexAttributes(STRIDE);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        vaoIds.Add(vaoId);
        indexCounts.Add(indices.Length);
        vboIds.Add(vbo);
        eboIds.Add(ebo);
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
        int location = shaderProgram.GetAttribLocation(attribName);
        GL.EnableVertexAttribArray((uint)location);
        GL.VertexAttribPointer((uint)location, size, VertexAttribPointerType.Float, false, stride, offset);
    }

    public void RenderVAO(int index)
    {
        if (index < 0 || index >= vaoIds.Count) return;
        GL.BindVertexArray(vaoIds[index]);
        GL.DrawElements(PrimitiveType.Triangles, indexCounts[index], DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void DeleteVAOs()
    {
        foreach (var vao in vaoIds) GL.DeleteVertexArray(vao);
        foreach (var vbo in vboIds) GL.DeleteBuffer(vbo);
        foreach (var ebo in eboIds) GL.DeleteBuffer(ebo);

        vaoIds.Clear();
        vboIds.Clear();
        eboIds.Clear();
        indexCounts.Clear();
    }

    public void DeleteVAO(int index)
    {
        if (index < 0 || index >= vaoIds.Count)
        {
            Console.WriteLine($"Index {index} is out of bounds for VAOs.");
            return;
        }

        GL.DeleteVertexArray(vaoIds[index]);
        GL.DeleteBuffer(vboIds[index]);
        GL.DeleteBuffer(eboIds[index]);

        vaoIds.RemoveAt(index);
        vboIds.RemoveAt(index);
        eboIds.RemoveAt(index);
        indexCounts.RemoveAt(index);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                DeleteVAOs();
            }

            disposed = true;
        }
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