namespace GraphicsEngine_OpenGL;

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
