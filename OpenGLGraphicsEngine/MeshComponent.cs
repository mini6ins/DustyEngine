using Assimp;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;

public class MeshComponent
{
    // Geometry data
    public float[] Vertices { get; set; }
    public uint[] Indices { get; set; }
    
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.One;
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    public Material Material { get; set; } = new Material();

    public Matrix4 GetModelMatrix()
    {
        return Matrix4.CreateScale(Scale) 
               * Matrix4.CreateRotationX(Rotation.X)
               * Matrix4.CreateRotationY(Rotation.Y)
               * Matrix4.CreateRotationZ(Rotation.Z)
               * Matrix4.CreateTranslation(Position);
    }

     public void LoadFromObj(string filePath)
    {
        AssimpContext importer = new AssimpContext();
        
        // Добавляем флаг LoadMaterials для загрузки материалов
        Scene scene = importer.ImportFile(filePath, 
            PostProcessSteps.Triangulate | 
            PostProcessSteps.GenerateNormals | 
            PostProcessSteps.FlipUVs |
            PostProcessSteps.PreTransformVertices);

        if (scene == null || !scene.HasMeshes)
        {
            throw new Exception("Ошибка загрузки OBJ: Нет мешей в файле.");
        }

        Mesh mesh = scene.Meshes[0]; // Берём первый меш в модели

        // Загружаем геометрию
        LoadGeometry(mesh);

        // Загружаем материал, если он есть
        if (scene.HasMaterials && mesh.MaterialIndex >= 0)
        {
            LoadMaterial(scene.Materials[mesh.MaterialIndex]);
        }
    }

    private void LoadGeometry(Mesh mesh)
    {
        List<float> vertexData = new List<float>();
        List<uint> indexData = new List<uint>();

        // Читаем вершины
        for (int i = 0; i < mesh.VertexCount; i++)
        {
            var vertex = mesh.Vertices[i];
            var normal = mesh.HasNormals ? mesh.Normals[i] : new Vector3D(0, 0, 0);
            var texCoord = mesh.HasTextureCoords(0) ? mesh.TextureCoordinateChannels[0][i] : new Vector3D(0, 0, 0);

            // Позиция
            vertexData.Add(vertex.X);
            vertexData.Add(vertex.Y);
            vertexData.Add(vertex.Z);

            // Цвет (будет перезаписан из материала)
            vertexData.Add(1.0f);
            vertexData.Add(1.0f);
            vertexData.Add(1.0f);
            vertexData.Add(1.0f);

            // UV координаты
            vertexData.Add(texCoord.X);
            vertexData.Add(texCoord.Y);

            // Нормали
            vertexData.Add(normal.X);
            vertexData.Add(normal.Y);
            vertexData.Add(normal.Z);
        }

        // Читаем индексы
        for (int i = 0; i < mesh.FaceCount; i++)
        {
            var face = mesh.Faces[i];
            if (face.IndexCount == 3)
            {
                indexData.Add((uint)face.Indices[0]);
                indexData.Add((uint)face.Indices[1]);
                indexData.Add((uint)face.Indices[2]);
            }
        }

        Vertices = vertexData.ToArray();
        Indices = indexData.ToArray();
    }

    private void LoadMaterial(Assimp.Material material)
    {
        // Создаём новый материал, если его нет
        if (Material == null)
        {
            Material = new Material();
        }

        // Загружаем цвета
        if (material.HasColorDiffuse)
        {
            Color4D diffuse = material.ColorDiffuse;
            Material.DiffuseColor = new float[] { diffuse.R, diffuse.G, diffuse.B };
        }

        if (material.HasColorAmbient)
        {
            Color4D ambient = material.ColorAmbient;
            Material.AmbientColor = new float[] { ambient.R, ambient.G, ambient.B };
        }

        if (material.HasColorSpecular)
        {
            Color4D specular = material.ColorSpecular;
            Material.SpecularColor = new float[] { specular.R, specular.G, specular.B };
        }

        // Загружаем shininess
        if (material.HasShininess)
        {
            Material.Shininess = material.Shininess;
        }

        // Загружаем пути к текстурам
        if (material.HasTextureDiffuse)
        {
            TextureSlot diffuseTexture;
            material.GetMaterialTexture(TextureType.Diffuse, 0, out diffuseTexture);
            Material.DiffuseTexturePath = diffuseTexture.FilePath;
        }

        if (material.HasTextureSpecular)
        {
            TextureSlot specularTexture;
            material.GetMaterialTexture(TextureType.Specular, 0, out specularTexture);
            Material.SpecularTexturePath = specularTexture.FilePath;
        }

        if (material.HasTextureHeight)
        {
            TextureSlot normalTexture;
            material.GetMaterialTexture(TextureType.Height, 0, out normalTexture);
            Material.NormalTexturePath = normalTexture.FilePath;
        }

        // Загружаем текстуры
        Material.LoadTextures();

        // Применяем цвета к вершинам, если нет текстуры
        if (!Material.HasDiffuseTexture)
        {
            ApplyColorToVertices(Material.DiffuseColor);
        }
    }

    private void ApplyColorToVertices(float[] color)
    {
        for (int i = 0; i < Vertices.Length; i += 12) // 12 - количество float на вершину
        {
            Vertices[i + 3] = color[0]; // R
            Vertices[i + 4] = color[1]; // G
            Vertices[i + 5] = color[2]; // B
            Vertices[i + 6] = 1.0f;     // A
        }
    }


    public void Translate(Vector3 translation)
    {
        Position += translation;
    }
}

public class Material
{
    // Basic material properties
    public float[] DiffuseColor { get; set; } = new float[] { 1.0f, 1.0f, 1.0f };  // Kd
    public float[] AmbientColor { get; set; } = new float[] { 0.2f, 0.2f, 0.2f };  // Ka
    public float[] SpecularColor { get; set; } = new float[] { 1.0f, 1.0f, 1.0f }; // Ks
    public float Shininess { get; set; } = 32.0f;                                   // Ns

    // Texture properties
    public int DiffuseTextureId { get; set; } = -1;     // GL texture ID for map_Kd
    public int SpecularTextureId { get; set; } = -1;    // GL texture ID for map_Ks
    public int NormalTextureId { get; set; } = -1;      // GL texture ID for map_Bump or map_Normal
    
    // Texture paths (load from  MTL)
    public string DiffuseTexturePath { get; set; } = string.Empty;    // map_Kd path
    public string SpecularTexturePath { get; set; } = string.Empty;   // map_Ks path
    public string NormalTexturePath { get; set; } = string.Empty;     // map_Bump path

    public bool HasDiffuseTexture => DiffuseTextureId != -1;
    public bool HasSpecularTexture => SpecularTextureId != -1;
    public bool HasNormalTexture => NormalTextureId != -1;

    public void LoadTextures()
    {
        if (!string.IsNullOrEmpty(DiffuseTexturePath))
        {
            DiffuseTextureId = LoadTexture(DiffuseTexturePath);
        }
        if (!string.IsNullOrEmpty(SpecularTexturePath))
        {
            SpecularTextureId = LoadTexture(SpecularTexturePath);
        }
        if (!string.IsNullOrEmpty(NormalTexturePath))
        {
            NormalTextureId = LoadTexture(NormalTexturePath);
        }
    }

    private int LoadTexture(string path)
    {
        try
        {
            var texture = new Texture(path);
            return texture.Handle;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load texture {path}: {ex.Message}");
            return -1;
        }
    }

    public void Dispose()
    {
        if (DiffuseTextureId != -1) GL.DeleteTexture(DiffuseTextureId);
        if (SpecularTextureId != -1) GL.DeleteTexture(SpecularTextureId);
        if (NormalTextureId != -1) GL.DeleteTexture(NormalTextureId);
    }
}