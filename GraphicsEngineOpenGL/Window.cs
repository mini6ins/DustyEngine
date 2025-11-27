using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngineOpenGL.RenderUtils;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Utils;
using Vector3 = DustyEngine.Engine.Math.Vectors.Vector3;
using DustyEngine.Runner;
using System.IO.Pipes;
using StreamJsonRpc;
using Buffer = System.Buffer;

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

public class Window : GameWindow
{
    private readonly string _windowName;

    private readonly List<Camera> _sceneCameras;
    private readonly EditorCamera? _editorCamera;
    private CameraBase ActiveCamera => ((_renderMode == RenderMode.Editor) && _editorCamera != null) ? _editorCamera : _sceneCameras.First();

    private Matrix4 _projection;
    
    private readonly CursorState _cursorState;
    private readonly RenderMode _renderMode;

    private readonly ShaderProgram _shaderProgram;
    private readonly List<VAOManager> _vaoList = [];
    private readonly List<RenderableObject> _sceneObjects = [];
    
    private readonly List<MeshRenderer> _allRenderers = [];

    private bool _initialized;

    // RPC Server для Editor mode (отдаем кадры клиентам)
    private Thread? _rpcServerThread;
    private volatile bool _rpcServerRunning = false;
    private int _connectedClients = 0;
    
    // Frame buffer для RPC клиентов
    private readonly FrameData[] _frameBuffer = new FrameData[3];
    private int _writeIndex = 0;
    private readonly object _bufferLock = new object();
    private System.Diagnostics.Stopwatch _frameStopwatch = System.Diagnostics.Stopwatch.StartNew();

    private float _edYaw = 0f;
    private float _edPitch = 0f;
    private float _edSpeed = 8f;
    private float _edMouseSensitivity = 0.15f;
    private float _edSmoothDX = 0f;
    private float _edSmoothDY = 0f;
    private const float _edSmoothing = 0.3f;

    public Window(GameWindowSettings gws, NativeWindowSettings nws, Scene scene,
        string vertShaderPath, string fragShaderPath, string windowName, bool isVsync = true, 
        CursorState cursorState = CursorState.Normal, RenderMode renderMode = RenderMode.Editor)
        : base(gws, nws)
    {
        _windowName = windowName;
        Title = _windowName;
        
        _cursorState = cursorState;
        _renderMode = renderMode;

        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        
        _shaderProgram = new ShaderProgram(vertShaderPath, fragShaderPath);

        _sceneCameras = SceneManager.FindCameras();
        
        if (renderMode == RenderMode.Editor)
        {
            var ec = new EditorCamera
            {
                AspectRatio = nws.ClientSize.X / (float)nws.ClientSize.Y
            };

            ec.InternalTransform.LocalPosition = new Vector3(0f, 2.5f, 5f);
            ec.InternalTransform.LocalRotation = new Vector3(0f, 0f, 0f);

            _editorCamera = ec;
        }
        
        _allRenderers.Clear();
        foreach (var obj in scene.GameObjects) 
            SceneManager.CollectMeshRenderers(obj, _allRenderers);
        
        Debug.Log($"Total Meshes: {_allRenderers.Count}", Debug.LogLevel.Info, true);
        
        foreach (var meshRenderer in _allRenderers)
            AddRenderer(meshRenderer);

        // Инициализация frame buffer для RPC
        if (_renderMode == RenderMode.Editor)
        {
            int pixelCount = nws.ClientSize.X * nws.ClientSize.Y * 4;
            for (int i = 0; i < _frameBuffer.Length; i++)
            {
                _frameBuffer[i] = new FrameData
                {
                    Width = nws.ClientSize.X,
                    Height = nws.ClientSize.Y,
                    PixelData = new byte[pixelCount]
                };
            }
        }
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
        if (mesh == null || mesh.Vertices == null || mesh.Indices == null ||
            mesh.Vertices.Length == 0 || mesh.Indices.Length == 0)
        {
            Debug.Log("[AddRenderer] Empty Mesh — skipping. " +
                      "Make sure MeshRenderer loads a valid mesh or assign a default one.");
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

    protected override void OnLoad()
    {
        base.OnLoad();

        if ((_renderMode == RenderMode.Editor) && _editorCamera != null)
        {
            var euler = _editorCamera.InternalTransform.LocalRotationQuat.ToEulerAngles();
            _edPitch = euler.X * (180f / MathF.PI);
            _edYaw = euler.Y * (180f / MathF.PI);
        }

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        CursorState = _cursorState;
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        ActiveCamera.AspectRatio = Size.X / (float)Size.Y;
        _projection = ActiveCamera.GetProjectionMatrix();

        // Запуск RPC сервера для Editor mode
        if (_renderMode == RenderMode.Editor)
        {
            StartRpcServer();
        }

        _initialized = true;
    }

    /// <summary>
    /// Запуск RPC сервера для отдачи кадров клиентам
    /// </summary>
    private void StartRpcServer()
    {
        _rpcServerRunning = true;
        _rpcServerThread = new Thread(RpcServerLoop)
        {
            Name = "RPC Server Thread",
            IsBackground = true
        };
        _rpcServerThread.Start();
        
        Console.WriteLine("===========================================");
        Console.WriteLine("RPC Server started!");
        Console.WriteLine("Clients can connect to: StreamJsonRpcSamplePipe");
        Console.WriteLine("===========================================");
    }

    /// <summary>
    /// RPC Server loop - принимает подключения клиентов
    /// </summary>
    private async void RpcServerLoop()
    {
        int clientId = 0;

        while (_rpcServerRunning)
        {
            try
            {
                Console.WriteLine($"[RPC Server] Waiting for client connection...");

                var stream = new NamedPipeServerStream("StreamJsonRpcSamplePipe", 
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances, 
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await stream.WaitForConnectionAsync();

                int currentClientId = ++clientId;
                _connectedClients++;
                Console.WriteLine($"[RPC Server] Client #{currentClientId} connected! Total clients: {_connectedClients}");
                
                // Обработка клиента в отдельной задаче
                _ = Task.Run(() => HandleRpcClient(stream, currentClientId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RPC Server] Error in main loop: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    /// <summary>
    /// Обработка RPC клиента
    /// </summary>
    private async Task HandleRpcClient(NamedPipeServerStream stream, int clientId)
    {
        try
        {
            await using (stream)
            {
                // Создаем изолированный RPC сервис
                var rpcService = new RpcService(
                    getFrameData: GetCurrentFrame,
                    onKeyPress: HandleKeyFromClient,
                    onMouseMove: HandleMouseMoveFromClient,
                    onMouseClick: HandleMouseClickFromClient
                );

                // Attach только RpcService, не Window!
                var jsonRpc = JsonRpc.Attach(stream, rpcService);
                Console.WriteLine($"[RPC Server] JSON-RPC attached for client #{clientId}");

                jsonRpc.Disconnected += (sender, args) =>
                {
                    _connectedClients--;
                    Console.WriteLine($"[RPC Server] Client #{clientId} disconnected: {args.Reason}. Remaining: {_connectedClients}");
                };

                await jsonRpc.Completion;
            }
        }
        catch (Exception ex)
        {
            _connectedClients--;
            Console.WriteLine($"[RPC Server] Error handling client #{clientId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить текущий кадр для RPC
    /// </summary>
    private FrameData GetCurrentFrame()
    {
        if (!_initialized)
        {
            return new FrameData
            {
                Width = FramebufferSize.X,
                Height = FramebufferSize.Y,
                PixelData = Array.Empty<byte>()
            };
        }

        // Читаем последний готовый кадр
        int safeReadIndex = (_writeIndex - 1 + _frameBuffer.Length) % _frameBuffer.Length;
        var source = _frameBuffer[safeReadIndex];

        var result = new FrameData
        {
            Width = source.Width,
            Height = source.Height,
            Timestamp = source.Timestamp,
            PixelData = new byte[source.PixelData.Length]
        };

        Buffer.BlockCopy(source.PixelData, 0, result.PixelData, 0, source.PixelData.Length);

        return result;
    }

    /// <summary>
    /// Обработка нажатия клавиши от RPC клиента
    /// </summary>
    private void HandleKeyFromClient(string key)
    {
        // TODO: можно передавать события в движок
    }

    /// <summary>
    /// Обработка движения мыши от RPC клиента
    /// </summary>
    private void HandleMouseMoveFromClient(float normalizedX, float normalizedY)
    {
        // TODO: можно передавать события в движок
    }

    /// <summary>
    /// Обработка клика мыши от RPC клиента
    /// </summary>
    private void HandleMouseClickFromClient(float normalizedX, float normalizedY, int button)
    {
        // TODO: можно передавать события в движок
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        Input.Update(KeyboardState);
        Input.UpdateMouseState(MouseState);
        
        EditorCameraMovement(args);
        
        // Обновляем заголовок с количеством клиентов
        if (_renderMode == RenderMode.Editor)
        {
            Title = $"{_windowName} - Editor Mode - RPC Clients: {_connectedClients}";
        }
    }

    private void EditorCameraMovement(FrameEventArgs args)
    {
        float dt = (float)args.Time;

        if ((_renderMode == RenderMode.Editor) && _editorCamera is EditorCamera ec)
        {
            if (Input.IsMouseButtonDown(MouseButton.Middle))
            {
                (float dx, float dy) = Input.Delta;
                _edSmoothDX = _edSmoothDX * _edSmoothing + dx * (1f - _edSmoothing);
                _edSmoothDY = _edSmoothDY * _edSmoothing + dy * (1f - _edSmoothing);

                const float deadZone = 0.001f;
                if (System.Math.Abs(_edSmoothDX) > deadZone || System.Math.Abs(_edSmoothDY) > deadZone)
                {
                    _edYaw -= _edSmoothDX * _edMouseSensitivity;
                    _edPitch -= _edSmoothDY * _edMouseSensitivity;
                    _edPitch = System.Math.Clamp(_edPitch, -89f, 89f);

                    float pitchRad = _edPitch * (MathF.PI / 180f);
                    float yawRad = _edYaw * (MathF.PI / 180f);

                    var currentRight = ec.InternalTransform.LocalRotationQuat.Rotate(
                        new Vector3(1f, 0f, 0f)
                    );
                    var qPitch = Quaternion.FromAxisAngle(currentRight, pitchRad);

                    var localUp = qPitch.Rotate(new Vector3(0f, 1f, 0f));
                    var qYaw = Quaternion.FromAxisAngle(localUp, yawRad);

                    ec.InternalTransform.LocalRotationQuat = qYaw * qPitch;
                }
            }

            var fwd = ec.InternalTransform.Forward;
            var right = ec.InternalTransform.Right;
            var up = ec.InternalTransform.Up;

            var dir = Vector3.Zero;
            if (Input.IsKeyDown(KeyCode.W)) dir += fwd;
            if (Input.IsKeyDown(KeyCode.S)) dir -= fwd;
            if (Input.IsKeyDown(KeyCode.A)) dir -= right;
            if (Input.IsKeyDown(KeyCode.D)) dir += right;
            if (Input.IsKeyDown(KeyCode.Space)) dir += up;
            if (Input.IsKeyDown(KeyCode.LeftShift)) dir -= up;

            if (dir.LengthSquared > 0f)
            {
                dir = dir.Normalized();
                ec.InternalTransform.LocalPosition += dir * _edSpeed * dt;
            }

            Input.ResetMouse();
        }

        if (Input.IsKeyDown(KeyCode.F1)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        if (Input.IsKeyDown(KeyCode.F2)) GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (_renderMode is RenderMode.Standalone or RenderMode.Editor) 
            Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderScene();

        // Захват кадра для RPC клиентов (если они есть)
        if (_renderMode == RenderMode.Editor && _connectedClients > 0)
        {
            CaptureFrameForRpc();
        }

        SwapBuffers();
    }

    /// <summary>
    /// Захват кадра для отправки RPC клиентам
    /// </summary>
    private void CaptureFrameForRpc()
    {
        try
        {
            float currentTime = (float)_frameStopwatch.Elapsed.TotalSeconds;
            int currentWrite = _writeIndex;

            // Читаем пиксели из текущего framebuffer
            GL.ReadPixels(0, 0, FramebufferSize.X, FramebufferSize.Y,
                PixelFormat.Rgba, PixelType.UnsignedByte, _frameBuffer[currentWrite].PixelData);

            _frameBuffer[currentWrite].Timestamp = currentTime;
            _frameBuffer[currentWrite].Width = FramebufferSize.X;
            _frameBuffer[currentWrite].Height = FramebufferSize.Y;

            _writeIndex = (currentWrite + 1) % _frameBuffer.Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC] Error capturing frame: {ex.Message}");
        }
    }

    private void RenderScene()
    {
        _shaderProgram.ActiveProgram();

        var viewMatrix = ActiveCamera.GetViewMatrix();
        _shaderProgram.SetUniform("uView", viewMatrix);
        _shaderProgram.SetUniform("uProjection", _projection);

        foreach (var obj in _sceneObjects)
            if (obj.MeshRenderer.IsActiveAndEnabled)
                RenderObject(obj);

        _shaderProgram.DeactiveProgram();
    }

    private void RenderObject(RenderableObject obj)
    {
        var transform = obj.Transform;

        Matrix4 rotation =
            Matrix4.CreateRotationX(transform.GlobalRotation.X) *
            Matrix4.CreateRotationY(transform.GlobalRotation.Y) *
            Matrix4.CreateRotationZ(transform.GlobalRotation.Z);

        Matrix4 modelMatrix =
            Matrix4.CreateScale(transform.GlobalScale.ToOpenTK()) *
            rotation *
            Matrix4.CreateTranslation(transform.GlobalPosition.ToOpenTK());

        _shaderProgram.SetUniform("uModel", modelMatrix);

        if (obj.VaoIndex < _vaoList.Count)
            _vaoList[obj.VaoIndex].RenderVAO(0);
    }

    protected override void OnUnload()
    {
        if (!_initialized) return;

        // Остановка RPC сервера
        _rpcServerRunning = false;
        _rpcServerThread?.Join(TimeSpan.FromSeconds(2));

        foreach (var vao in _vaoList)
            vao.Dispose();

        _shaderProgram.DeleteProgram();

        _initialized = false;
        base.OnUnload();
    }
}