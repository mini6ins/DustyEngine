using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Window : GameWindow
{
    private Matrix4 _projectionMatrix;
    private VAOManager _vaoManager;
    private ShaderProgram _shaderProgram;
    private ShaderProgram _debugShaderProgram;
    private List<MeshComponent> _meshComponents;
    private List<MeshComponent> _lightDebugCubes;
    private Camera _camera;
    private bool _is2D;
    private bool _isMovingLastMesh = false;

    private Vector3[] _pointLightPositions = new Vector3[]
    {
        new Vector3(0.7f, 0.2f, 2.0f),
        new Vector3(2.3f, -3.3f, -4.0f),
        new Vector3(-4.0f, 2.0f, -12.0f),
        new Vector3(0.0f, 0.0f, -3.0f)
    };

    private MeshComponent CubeMesh = new MeshComponent
    {
        Vertices = new float[]
        {
            // Front face (x, y, z, r, g, b, a, texU, texV, normX, normY, normZ)
            -0.5f, -0.5f, 0.5f, 1, 0, 0, 1, 0, 0, 0, 0, 1,
            0.5f, -0.5f, 0.5f, 1, 0, 0, 1, 1, 0, 0, 0, 1,
            0.5f, 0.5f, 0.5f, 1, 0, 0, 1, 1, 1, 0, 0, 1,
            -0.5f, 0.5f, 0.5f, 1, 0, 0, 1, 0, 1, 0, 0, 1,

            // Back face
            -0.5f, -0.5f, -0.5f, 0, 1, 0, 1, 1, 0, 0, 0, -1,
            0.5f, -0.5f, -0.5f, 0, 1, 0, 1, 0, 0, 0, 0, -1,
            0.5f, 0.5f, -0.5f, 0, 1, 0, 1, 0, 1, 0, 0, -1,
            -0.5f, 0.5f, -0.5f, 0, 1, 0, 1, 1, 1, 0, 0, -1,

            // Left face
            -0.5f, -0.5f, -0.5f, 0, 0, 1, 1, 0, 0, -1, 0, 0,
            -0.5f, -0.5f, 0.5f, 0, 0, 1, 1, 1, 0, -1, 0, 0,
            -0.5f, 0.5f, 0.5f, 0, 0, 1, 1, 1, 1, -1, 0, 0,
            -0.5f, 0.5f, -0.5f, 0, 0, 1, 1, 0, 1, -1, 0, 0,

            // Right face
            0.5f, -0.5f, -0.5f, 1, 1, 0, 1, 1, 0, 1, 0, 0,
            0.5f, -0.5f, 0.5f, 1, 1, 0, 1, 0, 0, 1, 0, 0,
            0.5f, 0.5f, 0.5f, 1, 1, 0, 1, 0, 1, 1, 0, 0,
            0.5f, 0.5f, -0.5f, 1, 1, 0, 1, 1, 1, 1, 0, 0,

            // Top face
            -0.5f, 0.5f, -0.5f, 1, 0, 1, 1, 0, 1, 0, 1, 0,
            0.5f, 0.5f, -0.5f, 1, 0, 1, 1, 1, 1, 0, 1, 0,
            0.5f, 0.5f, 0.5f, 1, 0, 1, 1, 1, 0, 0, 1, 0,
            -0.5f, 0.5f, 0.5f, 1, 0, 1, 1, 0, 0, 0, 1, 0,

            // Bottom face
            -0.5f, -0.5f, -0.5f, 0, 1, 1, 1, 0, 0, 0, -1, 0,
            0.5f, -0.5f, -0.5f, 0, 1, 1, 1, 1, 0, 0, -1, 0,
            0.5f, -0.5f, 0.5f, 0, 1, 1, 1, 1, 1, 0, -1, 0,
            -0.5f, -0.5f, 0.5f, 0, 1, 1, 1, 0, 1, 0, -1, 0
        },
        Indices = new uint[]
        {
            0, 1, 2, 0, 2, 3, // Front
            4, 6, 5, 4, 7, 6, // Back
            8, 9, 10, 8, 10, 11, // Left
            12, 14, 13, 12, 15, 14, // Right
            16, 18, 17, 16, 19, 18, // Top
            20, 21, 22, 20, 22, 23 // Bottom
        }
    };

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings,
        List<MeshComponent> meshComponents, bool is2D)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _meshComponents = meshComponents ?? new List<MeshComponent>();
        _lightDebugCubes = new List<MeshComponent>();
        VSync = VSyncMode.On;
        CursorState = CursorState.Grabbed;
        _camera = new Camera(new Vector3(0.0f, 0.0f, 0.0f), is2D);
        _is2D = is2D;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        GL.ClearColor(0.5f, 0.7f, 1.0f, 1.0f);

        _shaderProgram = new ShaderProgram(
            "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/Window/GraphicsTools/Shaders/shader.vert",
            "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/Window/GraphicsTools/Shaders/shader.frag");

        _debugShaderProgram = new ShaderProgram(
            "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/Window/GraphicsTools/Shaders/_Debug/debug.vert",
            "/home/maksym/OpenGLGraphicsEngine/OpenGLGraphicsEngine/Window/GraphicsTools/Shaders/_Debug/debug.frag");

        _vaoManager = new VAOManager(_shaderProgram);

        ConfigureOpenGLSettings();
        SetupProjectionMatrix();
        CreateInitialVAOs();
        LoadMeshTextures();
        CreateLightDebugCubes();
    }

    private void ConfigureOpenGLSettings()
    {
        if (!_is2D)
        {
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.Enable(EnableCap.DepthTest);
        }
        else
        {
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
        }
    }

    private void SetupProjectionMatrix()
    {
        _projectionMatrix = _is2D
            ? Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1.0f, 10000.0f)
            : Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                Size.X / (float)Size.Y,
                0.1f,
                10000.0f
            );
    }

    private void CreateInitialVAOs()
    {
        foreach (var meshComponent in _meshComponents)
        {
            _vaoManager.CreateVAO(meshComponent.Vertices, meshComponent.Indices);
        }
    }

    private void LoadMeshTextures()
    {
        // Make sure each mesh has its textures loaded
        foreach (var mesh in _meshComponents)
        {
            if (mesh.Material != null)
            {
                mesh.Material.LoadTextures();
            }
        }

        // Set texture uniform locations in the shader
        _shaderProgram.Use();
        int diffuseLocation = _shaderProgram.GetUniformLocation("material.diffuse");
        int specularLocation = _shaderProgram.GetUniformLocation("material.specular");
        GL.Uniform1i(diffuseLocation, 0);  // Texture unit 0 for diffuse
        GL.Uniform1i(specularLocation, 1); // Texture unit 1 for specular
        _shaderProgram.Deactivate();
    }

    private void CreateLightDebugCubes()
    {
        foreach (var lightPos in _pointLightPositions)
        {
            var lightCube = new MeshComponent
            {
                Vertices = CreateDebugCubeVertices(),
                Indices = CubeMesh.Indices,
                Position = lightPos,
                Scale = new Vector3(0.2f)
            };
            _lightDebugCubes.Add(lightCube);
            _vaoManager.CreateVAO(lightCube.Vertices, lightCube.Indices);
        }
    }

    private float[] CreateDebugCubeVertices()
    {
        // Create a completely new array of vertices
        var vertices = new float[CubeMesh.Vertices.Length];

        // Copy vertex positions and set the same color for all vertices
        for (int i = 0; i < vertices.Length; i += 12)
        {
            // Copy coordinates (x, y, z)
            vertices[i] = CubeMesh.Vertices[i]; // x
            vertices[i + 1] = CubeMesh.Vertices[i + 1]; // y
            vertices[i + 2] = CubeMesh.Vertices[i + 2]; // z

            // Set yellow color (r, g, b, a)
            vertices[i + 3] = 1.0f; // r (red)
            vertices[i + 4] = 1.0f; // g (green)
            vertices[i + 5] = 0.0f; // b (blue)
            vertices[i + 6] = 1.0f; // a (opacity)

            // Copy texture coordinates (u, v)
            vertices[i + 7] = CubeMesh.Vertices[i + 7];
            vertices[i + 8] = CubeMesh.Vertices[i + 8];

            // Copy normals (nx, ny, nz)
            vertices[i + 9] = CubeMesh.Vertices[i + 9];
            vertices[i + 10] = CubeMesh.Vertices[i + 10];
            vertices[i + 11] = CubeMesh.Vertices[i + 11];
        }

        return vertices;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Render regular objects
        _shaderProgram.Use();
        SetupShaderUniforms();
        RenderMeshComponents();
        _shaderProgram.Deactivate();

        // Render debug cubes
        _debugShaderProgram.Use();
        _debugShaderProgram.SetUniform("uProjectionMatrix", _projectionMatrix);
        _debugShaderProgram.SetUniform("uModelViewMatrix", _camera.GetViewMatrix());

        for (int i = 0; i < _lightDebugCubes.Count; i++)
        {
            _debugShaderProgram.SetUniform("uObjectPosition", _lightDebugCubes[i].Position);
            _debugShaderProgram.SetUniform("uObjectScale", _lightDebugCubes[i].Scale);
            _vaoManager.RenderVAOs(_meshComponents.Count + i);
        }

        _debugShaderProgram.Deactivate();

        SwapBuffers();
        base.OnRenderFrame(args);
    }

    private void SetupShaderUniforms()
    {
        _shaderProgram.SetUniform("uProjectionMatrix", _projectionMatrix);
        _shaderProgram.SetUniform("uModelViewMatrix", _camera.GetViewMatrix());
        _shaderProgram.SetUniform("viewPos", _camera.Position);

        SetupLightUniforms();
    }

    private void SetupLightUniforms()
    {
        // Directional Light
        _shaderProgram.SetUniform("dirLight.direction", new Vector3(-0.2f, -1.0f, -0.3f));
        _shaderProgram.SetUniform("dirLight.ambient", new Vector3(0.05f, 0.05f, 0.05f));
        _shaderProgram.SetUniform("dirLight.diffuse", new Vector3(0.4f, 0.4f, 0.4f));
        _shaderProgram.SetUniform("dirLight.specular", new Vector3(0.5f, 0.5f, 0.5f));

        // Point Lights
        for (int i = 0; i < _pointLightPositions.Length; i++)
        {
            _shaderProgram.SetUniform($"pointLights[{i}].position", _pointLightPositions[i]);
            _shaderProgram.SetUniform($"pointLights[{i}].ambient", new Vector3(0.05f, 0.05f, 0.05f));
            _shaderProgram.SetUniform($"pointLights[{i}].diffuse", new Vector3(0.8f, 0.8f, 0.8f));
            _shaderProgram.SetUniform($"pointLights[{i}].specular", new Vector3(1.0f, 1.0f, 1.0f));
            _shaderProgram.SetUniform($"pointLights[{i}].constant", 1.0f);
            _shaderProgram.SetUniform($"pointLights[{i}].linear", 0.09f);
            _shaderProgram.SetUniform($"pointLights[{i}].quadratic", 0.032f);
        }

        // Spot Light
        // _shaderProgram.SetUniform("spotLight.position", _camera.Position);
        // _shaderProgram.SetUniform("spotLight.direction", _camera.Front);
        _shaderProgram.SetUniform("spotLight.ambient", new Vector3(0.0f, 0.0f, 0.0f));
        _shaderProgram.SetUniform("spotLight.diffuse", new Vector3(1.0f, 1.0f, 1.0f));
        _shaderProgram.SetUniform("spotLight.specular", new Vector3(1.0f, 1.0f, 1.0f));
        _shaderProgram.SetUniform("spotLight.constant", 1.0f);
        _shaderProgram.SetUniform("spotLight.linear", 0.09f);
        _shaderProgram.SetUniform("spotLight.quadratic", 0.032f);
        _shaderProgram.SetUniform("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
        _shaderProgram.SetUniform("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)));
    }

    private void RenderMeshComponents()
    {
        for (int i = 0; i < _meshComponents.Count; i++)
        {
            var mesh = _meshComponents[i];
            _shaderProgram.SetUniform("uObjectPosition", mesh.Position);
            _shaderProgram.SetUniform("uObjectScale", mesh.Scale);
            
            // Set material properties for this specific mesh
            SetupMaterialUniforms(mesh.Material);
            
            // Bind the diffuse texture for this mesh
            GL.ActiveTexture(TextureUnit.Texture0);
            if (mesh.Material != null && mesh.Material.HasDiffuseTexture)
            {
                GL.BindTexture(TextureTarget.Texture2d, mesh.Material.DiffuseTextureId);
            }
            else
            {
                // Bind a default texture or use a plain color
                GL.BindTexture(TextureTarget.Texture2d, 0);
            }
            
            // Bind the specular texture for this mesh if it exists
            GL.ActiveTexture(TextureUnit.Texture1);
            if (mesh.Material != null && mesh.Material.HasSpecularTexture)
            {
                GL.BindTexture(TextureTarget.Texture2d, mesh.Material.SpecularTextureId);
            }
            else
            {
                // Bind a default texture or use a plain color
                GL.BindTexture(TextureTarget.Texture2d, 0);
            }
            
            // Render this mesh
            _vaoManager.RenderVAOs(i);
        }
    }

    private void SetupMaterialUniforms(Material material)
    {
        if (material == null)
        {
            // Default material properties
      _shaderProgram.SetUniform("material.ambient", new Vector3(0.2f, 0.2f, 0.2f));
            _shaderProgram.SetUniform("material.diffuse", 0);
            _shaderProgram.SetUniform("material.specular", 1);
            _shaderProgram.SetUniform("material.shininess", 32.0f);
            return;
        }

        // Set material properties from the material object
        _shaderProgram.SetUniform("material.ambient", new Vector3(
            material.AmbientColor[0],
            material.AmbientColor[1],
            material.AmbientColor[2]));
            
        _shaderProgram.SetUniform("material.diffuse", 0);  // Texture unit 0
        _shaderProgram.SetUniform("material.specular", 1); // Texture unit 1
        
        // If no specular texture, use the color
        if (!material.HasSpecularTexture)
        {
            _shaderProgram.SetUniform("material.specularColor", new Vector3(
                material.SpecularColor[0],
                material.SpecularColor[1],
                material.SpecularColor[2]));
        }
        
        _shaderProgram.SetUniform("material.shininess", material.Shininess);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        var keyboard = KeyboardState;
        var mouse = MouseState;
        HandleDebugInput(keyboard, mouse, args);
        base.OnUpdateFrame(args);
    }

    private void HandleDebugInput(KeyboardState keyboard, MouseState mouse, FrameEventArgs args)
    {
        _camera.ProcessKeyboard(keyboard, (float)args.Time);
        _camera.ProcessMouseMove(mouse, (float)args.Time);

        if (keyboard.IsKeyPressed(Keys.Escape)) Close();
        if (keyboard.IsKeyPressed(Keys.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        if (keyboard.IsKeyPressed(Keys.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        HandleMeshManipulation(keyboard);
        HandleMeshMovement(keyboard);
    }

    private void HandleMeshManipulation(KeyboardState keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.F3))
        {
            var newCube = new MeshComponent
            {
                Vertices = CubeMesh.Vertices,
                Indices = CubeMesh.Indices,
                Position = new Vector3(0, 0, 0),
                Material = new Material()
            };

            // Set a default texture path - you might want to select differently
            string texturePath = $"C:\\Users\\Maksym\\Documents\\GitHub\\OpenGLGraphicsEngine\\OpenGLGraphicsEngine\\CubeOBJ\\texture{_meshComponents.Count % 3 + 1}.jpg";
            newCube.Material.DiffuseTexturePath = texturePath;
            newCube.Material.LoadTextures();

            _meshComponents.Add(newCube);
            _vaoManager.CreateVAO(newCube.Vertices, newCube.Indices);
            Console.WriteLine($"New object added with texture {texturePath}. Total objects: {_meshComponents.Count}");
        }

        if (keyboard.IsKeyPressed(Keys.F4) && _meshComponents.Count > 0)
        {
            int index = _meshComponents.Count - 1;
            
            // Dispose the material textures before removing
            if (_meshComponents[index].Material != null)
            {
                _meshComponents[index].Material.Dispose();
            }
            
            _vaoManager.DeleteVAO(index);
            _meshComponents.RemoveAt(index);
        }
    }

    private void HandleMeshMovement(KeyboardState keyboard)
    {
        _isMovingLastMesh = keyboard.IsKeyDown(Keys.F5);

        if (_isMovingLastMesh && _meshComponents.Count > 0)
        {
            var lastMesh = _meshComponents[^1];

            if (keyboard.IsKeyDown(Keys.I)) lastMesh.Translate(new Vector3(0, 0.01f, 0));
            if (keyboard.IsKeyDown(Keys.K)) lastMesh.Translate(new Vector3(0, -0.01f, 0));
            if (keyboard.IsKeyDown(Keys.J)) lastMesh.Translate(new Vector3(-0.01f, 0, 0));
            if (keyboard.IsKeyDown(Keys.L)) lastMesh.Translate(new Vector3(0.01f, 0, 0));
        }
    }

    protected override void OnUnload()
    {
        // Dispose all mesh materials
        foreach (var mesh in _meshComponents)
        {
            if (mesh.Material != null)
            {
                mesh.Material.Dispose();
            }
        }
        
        _vaoManager.DeleteVAOs();
        _shaderProgram.Dispose();
        _debugShaderProgram.Dispose();
        base.OnUnload();
    }
}