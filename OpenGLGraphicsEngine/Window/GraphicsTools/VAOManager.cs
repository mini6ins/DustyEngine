using OpenTK.Graphics.OpenGL.Compatibility;

public class VAOManager : IDisposable
{
    private ShaderProgram shaderProgram;
    private List<int> vaoIds = new List<int>();
    private List<int> indexCounts = new List<int>();
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

        // For cube mesh format: position(3) + color(4) + texCoord(2) + normal(3) = 12 floats per vertex
        const int STRIDE = 12 * sizeof(float);
        SetupVertexAttributes(STRIDE);

        GL.BindVertexArray(0);

        vaoIds.Add(vaoId);
        indexCounts.Add(indices.Length);
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
        SetupVertexAttribPointer("aTexCoord", 2, 7 * sizeof(float), stride);
        SetupVertexAttribPointer("aNormal", 3, 9 * sizeof(float), stride);
    }

    private void SetupVertexAttribPointer(string attribName, int size, int offset, int stride)
    {
        int location = shaderProgram.GetAttribLocation(attribName);
        if (location != -1)
        {
            GL.EnableVertexAttribArray((uint)location);
            GL.VertexAttribPointer((uint)location, size, VertexAttribPointerType.Float, false, stride, offset);
        }
    }

    public void RenderVAOs(int index)
    {
        if (index < 0 || index >= vaoIds.Count) return;
        GL.BindVertexArray(vaoIds[index]);
        GL.DrawElements(PrimitiveType.Triangles, indexCounts[index], DrawElementsType.UnsignedInt, 0);
    }

    public void DeleteVAOs()
    {
        foreach (var vaoId in vaoIds)
        {
            GL.DeleteVertexArray(vaoId);
        }
        vaoIds.Clear();
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
        vaoIds.RemoveAt(index);
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