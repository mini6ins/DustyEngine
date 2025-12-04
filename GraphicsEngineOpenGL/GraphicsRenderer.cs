using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngineOpenGL.RenderUtils;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Utils;
using MouseButton = Utils.MouseButton;
using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace GraphicsEngineOpenGL;

public class RenderableObject
{
    public int VaoIndex;
    public Transform Transform = new();
    public MeshRenderer MeshRenderer = null!;
}

public enum RenderMode
{
    Standalone,
    Editor
}

public class GraphicsRenderer
{
    private readonly string _vertShaderPath;
    private readonly string _fragShaderPath;
    private readonly int _viewportWidth;
    private readonly int _viewportHeight;
    private readonly RenderMode _renderMode;

    private ShaderProgram _shaderProgram = null!;
    private readonly List<VAOManager> _vaoList = [];
    private readonly List<RenderableObject> _sceneObjects = [];

    private List<Camera> _sceneCameras = null!;
    private EditorCamera? _editorCamera;
    private Matrix4 _projection;

    private CameraBase ActiveCamera => (IsEditorMode && _editorCamera != null)
        ? _editorCamera
        : _sceneCameras.First();

    private bool IsEditorMode => _renderMode == RenderMode.Editor;

    public GraphicsRenderer(string vertShaderPath, string fragShaderPath,
        int viewportWidth, int viewportHeight, RenderMode renderMode)
    {
        _vertShaderPath = vertShaderPath;
        _fragShaderPath = fragShaderPath;
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
        _renderMode = renderMode;
    }

    public void Load()
    {
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        
        _shaderProgram = new ShaderProgram(_vertShaderPath, _fragShaderPath);
        
        _sceneCameras = SceneManager.FindCameras();

        if (IsEditorMode)
        {
            _editorCamera = new EditorCamera
            {
                AspectRatio = _viewportWidth / (float)_viewportHeight,
                InternalTransform =
                {
                    LocalPosition = new Vector3(0f, 2.5f, 5f),
                    LocalRotation = new Vector3(0f, 0f, 0f)
                }
            };
        }

        ActiveCamera.AspectRatio = _viewportWidth / (float)_viewportHeight;
        _projection = ActiveCamera.GetProjectionMatrix();
        
        LoadSceneRenderers();
        
        if (IsEditorMode)
        {
            _editorCamera?.InitializeController();
            Input.Input.EnableRpcInput();
            Console.WriteLine("[Input] RPC input mode enabled for Editor");
        }
    }

    private void LoadSceneRenderers()
    {
        var allRenderers = new List<MeshRenderer>();

        if (SceneManager.CurrentScene == null) return;

        foreach (var obj in SceneManager.CurrentScene.GameObjects)
            SceneManager.CollectMeshRenderers(obj, allRenderers);

        Debug.Log($"Total Meshes: {allRenderers.Count}", Debug.LogLevel.Info, true);

        foreach (var meshRenderer in allRenderers)
            AddRenderer(meshRenderer);
    }

    public void Update(float deltaTime, KeyboardState keyboardState, MouseState mouseState)
    {
        if (!IsEditorMode)
        {
            Input.Input.Update(keyboardState);
            Input.Input.UpdateMouseState(mouseState);
        }
        else
        {
            UpdateEditorCamera(deltaTime);
        }

        HandleDebugInput();
    }

    public void OnMouseMove(float x, float y)
    {
        if (!IsEditorMode)
        {
            Input.Input.UpdateMouse(x, y);
        }
    }

    public void CaptureFrameIfNeeded(RpcController? rpcManager, int width, int height)
    {
        if (IsEditorMode && rpcManager?.ConnectedClients > 0)
        {
            rpcManager.CaptureFrame(width, height);
        }
    }

    private void UpdateEditorCamera(float deltaTime)
    {
        if (_editorCamera == null) return;

        var isMiddleMouseDown = Input.Input.IsMouseButtonDown(MouseButton.Middle);
        var mouseDelta = Input.Input.Delta;

        var movementInput = new MovementInput
        {
            Forward = Input.Input.IsKeyDown(KeyCode.W),
            Backward = Input.Input.IsKeyDown(KeyCode.S),
            Left = Input.Input.IsKeyDown(KeyCode.A),
            Right = Input.Input.IsKeyDown(KeyCode.D),
            Up = Input.Input.IsKeyDown(KeyCode.Space),
            Down = Input.Input.IsKeyDown(KeyCode.LeftShift)
        };

        _editorCamera.UpdateMovement(deltaTime, isMiddleMouseDown, mouseDelta, movementInput);
        
        if (Input.Input.IsRpcInputActive)
            Input.Input.RpcResetMouseDelta();
        else
            Input.Input.ResetMouse();
    }

    private void HandleDebugInput()
    {
        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F1))
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F2))
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    public void Render()
    {
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        _shaderProgram.ActiveProgram();
        var viewMatrix = ActiveCamera.GetViewMatrix();
        _shaderProgram.SetUniform("uView", viewMatrix);
        _shaderProgram.SetUniform("uProjection", _projection);

        foreach (var obj in _sceneObjects.Where(obj => obj.MeshRenderer.IsActiveAndEnabled))
            RenderObject(obj);

        _shaderProgram.DeactiveProgram();
    }

    private void RenderObject(RenderableObject obj)
    {
        var transform = obj.Transform;
        var rotation =
            Matrix4.CreateRotationX(transform.GlobalRotation.X) *
            Matrix4.CreateRotationY(transform.GlobalRotation.Y) *
            Matrix4.CreateRotationZ(transform.GlobalRotation.Z);
        var modelMatrix =
            Matrix4.CreateScale(transform.GlobalScale.ToOpenTK()) *
            rotation *
            Matrix4.CreateTranslation(transform.GlobalPosition.ToOpenTK());
        _shaderProgram.SetUniform("uModel", modelMatrix);
        if (obj.VaoIndex < _vaoList.Count)
            _vaoList[obj.VaoIndex].RenderVAO(0);
    }

    public void AddRenderer(MeshRenderer? meshRenderer)
    {
        if (meshRenderer == null)
        {
            Debug.Log("[AddRenderer] meshRenderer == null — skipping.");
            return;
        }

        meshRenderer.EnsureLoaded();
        var mesh = meshRenderer.GetMesh();

        if (mesh == null || mesh.Vertices.Length == 0 || mesh.Indices.Length == 0)
        {
            Debug.Log("[AddRenderer] Empty Mesh — skipping.");
            return;
        }

        if (meshRenderer.Parent == null)
        {
            Debug.Log("[AddRenderer] meshRenderer.Parent == null — skipping.");
            return;
        }

        var transform = meshRenderer.Parent.GetComponent<Transform>();
        if (transform == null)
        {
            Debug.Log("[AddRenderer] No Transform found on parent — skipping.");
            return;
        }

        var vao = new VAOManager(_shaderProgram);
        vao.CreateVAO(mesh.Vertices, mesh.Indices);
        _vaoList.Add(vao);
        _sceneObjects.Add(new RenderableObject
        {
            VaoIndex = _vaoList.Count - 1,
            Transform = transform,
            MeshRenderer = meshRenderer,
        });
    }

    public bool RemoveRenderer(int objectId)
    {
        if (objectId < 0 || objectId >= _sceneObjects.Count) return false;
        var obj = _sceneObjects[objectId];
        if (obj.VaoIndex < _vaoList.Count)
        {
            _vaoList[obj.VaoIndex].Dispose();
            _vaoList.RemoveAt(obj.VaoIndex);
            foreach (var t in _sceneObjects.Where(t => t.VaoIndex > obj.VaoIndex))
                t.VaoIndex--;
        }

        _sceneObjects.RemoveAt(objectId);
        return true;
    }

    public void Dispose()
    {
        if (IsEditorMode)
        {
            Input.Input.DisableRpcInput();
        }

        foreach (var vao in _vaoList)
            vao.Dispose();
        _shaderProgram?.DeleteProgram();
    }
}