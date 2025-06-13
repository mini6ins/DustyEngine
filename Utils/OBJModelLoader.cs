using Assimp;

namespace Utils;

public class OBJModelLoader
{
    public static bool LoadModel(string path, out float[] vertices, out uint[] indices)
    {
        var importer = new AssimpContext();
        var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs);

        if (scene.MeshCount == 0)
        {
            throw new Exception("No meshes found in model.");
        }

        var mesh = scene.Meshes[0];

        var vertList = new List<float>();
        for (var i = 0; i < mesh.Vertices.Count; i++)
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

        vertices = vertList.ToArray();
        indices = (from face in mesh.Faces from i in face.Indices select (uint)i).ToArray();
        return true;
    }
}