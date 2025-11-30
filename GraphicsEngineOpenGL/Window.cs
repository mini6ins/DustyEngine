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

    // RPC Server
    private Thread? _rpcServerThread;
    private volatile bool _rpcServerRunning = false;
    private int _connectedClients = 0;
    
    // Frame buffer
    private class FrameSlot
    {
        public FrameData Frame = new();
        public volatile bool IsReady = false;
    }
    
    private readonly FrameSlot[] _frameSlots = new FrameSlot[2]; // ✅ Двойная буферизация
    private volatile int _latestFrameIndex = 0;
    private readonly object _frameLock = new object();
    private System.Diagnostics.Stopwatch _frameStopwatch = System.Diagnostics.Stopwatch.StartNew();
    // Camera
    private float _edYaw = 0f;
    private float _edPitch = 0f;
    private float _edSpeed = 8f;
    private float _edMouseSensitivity = 0.15f;
    private float _edSmoothDX = 0f;
    private float _edSmoothDY = 0f;
    private const float _edSmoothing = 0.7f; // ✅ Увеличено с 0.3 до 0.7 для большей плавности

    

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

         
        if (_renderMode == RenderMode.Editor)
        {
            int pixelCount = nws.ClientSize.X * nws.ClientSize.Y * 4;
            for (int i = 0; i < _frameSlots.Length; i++)
            {
                _frameSlots[i] = new FrameSlot
                {
                    Frame = new FrameData
                    {
                        Width = nws.ClientSize.X,
                        Height = nws.ClientSize.Y,
                        PixelData = new byte[pixelCount]
                    }
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
        
        if (_renderMode == RenderMode.Editor)
        {
            StartRpcServer();
            // Включаем RPC ввод сразу при запуске в режиме Editor
            Input.Input.EnableRpcInput();
            Console.WriteLine("[Input] RPC input mode enabled for Editor");
        }
        
        _initialized = true;
    }

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
                _ = Task.Run(() => HandleRpcClient(stream, currentClientId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RPC Server] Error in main loop: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    private async Task HandleRpcClient(NamedPipeServerStream stream, int clientId)
    {
        try
        {
            await using (stream)
            {
                var rpcService = new RpcService(
                    getFrameData: GetCurrentFrame,
                    onKeyEvent: HandleKeyEvent,
                    onMouseMove: HandleMouseMoveFromClient,
                    onMouseEvent: HandleMouseEvent
                );
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
        
        var slot = _frameSlots[_latestFrameIndex];
        
        if (!slot.IsReady)
        {
            return new FrameData
            {
                Width = FramebufferSize.X,
                Height = FramebufferSize.Y,
                PixelData = Array.Empty<byte>()
            };
        }
        
        var source = slot.Frame;
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

    private void HandleKeyEvent(string key, bool isPressed)
    {
        if (isPressed)
        {
            Input.Input.RpcKeyDown(key);
        }
        else
        {
            Input.Input.RpcKeyUp(key);
        }
    }

    private void HandleMouseMoveFromClient(float normalizedX, float normalizedY)
    {
        Input.Input.RpcMouseMove(normalizedX, normalizedY);
    }

    private void HandleMouseEvent(float normalizedX, float normalizedY, int button, bool isPressed)
    {
        MouseButton mouseButton = button switch
        {
            0 => MouseButton.Left,
            1 => MouseButton.Right,
            2 => MouseButton.Middle,
            _ => MouseButton.Left
        };
        
        if (isPressed)
        {
            Input.Input.RpcMouseDown(mouseButton);
        }
        else
        {
            Input.Input.RpcMouseUp(mouseButton);
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        // В режиме Editor всегда используем RPC ввод
        // В режиме Standalone используем локальный ввод
        if (_renderMode == RenderMode.Editor)
        {
            // RPC ввод уже включен в OnLoad, ничего не делаем
        }
        else
        {
            // Standalone режим - обновляем локальный ввод
            Input.Input.Update(KeyboardState);
            Input.Input.UpdateMouseState(MouseState);
        }
        
        // Вызываем единый метод для движения камеры
        EditorCameraMovement(args);
        
        if (_renderMode == RenderMode.Editor)
        {
            string inputMode = Input.Input.IsRpcInputActive ? "RPC" : "Local";
            Title = $"{_windowName} - Editor - Clients: {_connectedClients} - Input: {inputMode}";
        }
    }



    private void EditorCameraMovement(FrameEventArgs args)
    {
        float dt = (float)args.Time;
        
        if ((_renderMode == RenderMode.Editor) && _editorCamera is EditorCamera ec)
        {
            bool shouldRotate = Input.Input.IsMouseButtonDown(MouseButton.Middle);
            
            if (shouldRotate)
            {
                (float dx, float dy) = Input.Input.Delta;
    
                // ✅ Применяем экспоненциальное сглаживание для плавности
                _edSmoothDX = _edSmoothDX * _edSmoothing + dx * (1f - _edSmoothing);
                _edSmoothDY = _edSmoothDY * _edSmoothing + dy * (1f - _edSmoothing);
    
                // ✅ Уменьшена мёртвая зона для более отзывчивого управления
                const float deadZone = 0.0001f;
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
            else
            {
                // ✅ Когда не вращаем, плавно затухаем остаточное движение
                _edSmoothDX *= _edSmoothing;
                _edSmoothDY *= _edSmoothing;
            }
            
            // Сбрасываем дельту после применения
            if (Input.Input.IsRpcInputActive)
            {
                Input.Input.RpcResetMouseDelta();
            }
            else
            {
                Input.Input.ResetMouse();
            }
            
            // Обработка движения камеры WASD + Space/Shift
            var fwd = ec.InternalTransform.Forward;
            var right = ec.InternalTransform.Right;
            var up = ec.InternalTransform.Up;
            var dir = Vector3.Zero;
            
            if (Input.Input.IsKeyDown(KeyCode.W)) dir += fwd;
            if (Input.Input.IsKeyDown(KeyCode.S)) dir -= fwd;
            if (Input.Input.IsKeyDown(KeyCode.A)) dir -= right;
            if (Input.Input.IsKeyDown(KeyCode.D)) dir += right;
            if (Input.Input.IsKeyDown(KeyCode.Space)) dir += up;
            if (Input.Input.IsKeyDown(KeyCode.LeftShift)) dir -= up;
            
            if (dir.LengthSquared > 0f)
            {
                dir = dir.Normalized();
                ec.InternalTransform.LocalPosition += dir * _edSpeed * dt;
            }
        }
        
        // Переключение режимов рендеринга
        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F1))
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        }
        if (Input.Input.IsKeyJustActivatedOnce(KeyCode.F2))
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        // В Standalone режиме обновляем локальную мышь
        if (_renderMode == RenderMode.Standalone) 
            Input.Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(173 / 255f, 216 / 255f, 230 / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        RenderScene();
        
        // ✅ Захват ПОСЛЕ рендеринга, перед SwapBuffers
        if (_renderMode == RenderMode.Editor && _connectedClients > 0)
        {
            CaptureFrameForRpc();
        }
        
        SwapBuffers();
    }

    private void CaptureFrameForRpc()
    {
        try
        {
            // Определяем индекс для записи (противоположный от читаемого)
            int writeIndex = 1 - _latestFrameIndex;
            var slot = _frameSlots[writeIndex];
            var targetBuffer = slot.Frame;
            
            // Проверка размера буфера
            int expectedSize = FramebufferSize.X * FramebufferSize.Y * 4;
            if (targetBuffer.PixelData.Length != expectedSize)
            {
                targetBuffer.PixelData = new byte[expectedSize];
            }
            
            // ✅ Захват кадра
            GL.ReadPixels(0, 0, FramebufferSize.X, FramebufferSize.Y,
                PixelFormat.Rgba, PixelType.UnsignedByte, targetBuffer.PixelData);
            
            targetBuffer.Timestamp = (float)_frameStopwatch.Elapsed.TotalSeconds;
            targetBuffer.Width = FramebufferSize.X;
            targetBuffer.Height = FramebufferSize.Y;
            
            // ✅ Атомарное переключение буфера
            slot.IsReady = true;
            _latestFrameIndex = writeIndex;
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
        
        _rpcServerRunning = false;
        _rpcServerThread?.Join(TimeSpan.FromSeconds(2));
        
        if (_renderMode == RenderMode.Editor)
        {
            Input.Input.DisableRpcInput();
        }
        
        foreach (var vao in _vaoList)
            vao.Dispose();
        _shaderProgram.DeleteProgram();
        _initialized = false;
        base.OnUnload();
    }
}