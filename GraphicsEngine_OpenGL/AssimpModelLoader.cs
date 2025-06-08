using Assimp;
using System.Numerics;

namespace GraphicsEngine_OpenGL;

public class AssimpModelLoader
{
    public static void LoadModel(string path, out float[] vertices, out uint[] indices)
    {
        var importer = new AssimpContext();
        var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs);

        if (scene.MeshCount == 0)
            throw new Exception("No meshes found in model.");

        var mesh = scene.Meshes[0];

        var vertList = new List<float>();
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            vertList.Add(v.X);
            vertList.Add(v.Y);
            vertList.Add(v.Z);

            if (mesh.HasVertexColors(0))
            {
                var c = mesh.VertexColorChannels[0][i];
                vertList.Add(c.R);
                vertList.Add(c.G);
                vertList.Add(c.B);
                vertList.Add(c.A);
            }
            else
            {
                vertList.Add(1); // R
                vertList.Add(0); // G
                vertList.Add(0); // B
                vertList.Add(1); // A 

            }
        }

        var idxList = new List<uint>();
        foreach (var face in mesh.Faces)
        foreach (var i in face.Indices)
            idxList.Add((uint)i);

        vertices = vertList.ToArray();
        indices = idxList.ToArray();
    }
}
