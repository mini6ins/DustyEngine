using System.Net;
using System.Net.Sockets;
using OpenTK.Graphics.OpenGL.Compatibility;

public class FramebufferSenderTCP : IDisposable
{
    private TcpListener _tcpListener;
    private TcpClient _connectedClient;
    private NetworkStream _networkStream;
    private volatile bool _isRunning = true;
    private volatile bool _disposed = false;

    private readonly int _port;

    // Управление отправкой кадров
    private DateTime _lastFrameSent = DateTime.MinValue;
    private readonly TimeSpan _frameInterval;

    // События
    public event Action OnClientConnected;
    public event Action OnClientDisconnected;
    public event Action<string> OnError;

    public bool IsClientConnected => _connectedClient?.Connected == true && _networkStream?.CanWrite == true;
    public bool IsRunning => _isRunning && !_disposed;

    public FramebufferSenderTCP(int port = 8080, int targetFPS = 30)
    {
        _port = port;
        _frameInterval = TimeSpan.FromMilliseconds(1000.0 / targetFPS);
    }

    public async Task<bool> StartAsync()
    {
        if (_disposed || _tcpListener != null)
            return false;

        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, _port);
            _tcpListener.Start();
            Console.WriteLine($"FramebufferSender: Сервер запущен на порту {_port}");

            // Запускаем прослушивание подключений
            _ = Task.Run(ListenForClients);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSender: Ошибка запуска: {ex.Message}");
            OnError?.Invoke($"Ошибка запуска: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        _isRunning = false;

        try
        {
            _networkStream?.Close();
            _connectedClient?.Close();
            _tcpListener?.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSender: Ошибка остановки: {ex.Message}");
        }
    }

    private async Task ListenForClients()
    {
        while (_isRunning && !_disposed)
        {
            try
            {
                Console.WriteLine("FramebufferSender: Ожидание подключения...");
                _connectedClient = await _tcpListener.AcceptTcpClientAsync();
                _connectedClient.NoDelay = true;
                _connectedClient.ReceiveTimeout = 10000;
                _connectedClient.SendTimeout = 10000;

                _networkStream = _connectedClient.GetStream();
                Console.WriteLine("FramebufferSender: Клиент подключился!");
                OnClientConnected?.Invoke();

                // Ждем пока клиент подключен
                while (_connectedClient.Connected && _isRunning && !_disposed)
                {
                    await Task.Delay(100);
                }

                Console.WriteLine("FramebufferSender: Клиент отключился");
                OnClientDisconnected?.Invoke();

                _networkStream?.Close();
                _connectedClient?.Close();
                _networkStream = null;
                _connectedClient = null;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FramebufferSender: Ошибка сервера: {ex.Message}");
                OnError?.Invoke($"Ошибка сервера: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    /// <summary>
    /// Отправляет текущий framebuffer по сети
    /// </summary>
    /// <param name="framebufferId">ID framebuffer (0 для default framebuffer)</param>
    /// <param name="width">Ширина framebuffer</param>
    /// <param name="height">Высота framebuffer</param>
    /// <param name="flipVertically">Переворачивать ли изображение вертикально (обычно true для OpenGL)</param>
    /// <returns>True если успешно отправлен, false иначе</returns>
    public bool SendFramebuffer(int framebufferId, int width, int height, bool flipVertically = true)
    {
        if (!IsClientConnected || _disposed)
            return false;

        // Ограничиваем частоту отправки
        var now = DateTime.Now;
        if (now - _lastFrameSent < _frameInterval)
            return true; // Не ошибка, просто пропускаем кадр

        _lastFrameSent = now;

        try
        {
            // Читаем пиксели из framebuffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            byte[] pixels = new byte[width * height * 4]; // RGBA
            GL.ReadPixels(0, 0, width, height,
                PixelFormat.Rgba,
                PixelType.UnsignedByte, pixels);

            // Переворачиваем изображение если нужно
            if (flipVertically)
            {
                pixels = FlipImageVertically(pixels, width, height);
            }

            // Создаем FrameData и отправляем
            var frameData = new FrameData
            {
                Width = width,
                Height = height,
                PixelData = pixels
            };

            return SendFrameData(frameData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSender: Ошибка чтения framebuffer: {ex.Message}");
            OnError?.Invoke($"Ошибка чтения framebuffer: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Отправляет готовые данные кадра
    /// </summary>
    public bool SendFrameData(FrameData frameData)
    {
        if (!IsClientConnected || _disposed || frameData == null)
            return false;

        try
        {
            var data = frameData.ToBytes();
            _networkStream.Write(data, 0, data.Length);
            _networkStream.Flush();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSender: Ошибка отправки: {ex.Message}");
            OnError?.Invoke($"Ошибка отправки: {ex.Message}");

            // Закрываем соединение при ошибке
            try
            {
                _networkStream?.Close();
                _connectedClient?.Close();
            }
            catch
            {
            }

            _networkStream = null;
            _connectedClient = null;

            return false;
        }
    }

    /// <summary>
    /// Асинхронно отправляет framebuffer
    /// </summary>
    public async Task<bool> SendFramebufferAsync(int framebufferId, int width, int height, bool flipVertically = true)
    {
        if (!IsClientConnected || _disposed)
            return false;

        var now = DateTime.Now;
        if (now - _lastFrameSent < _frameInterval)
            return true;

        _lastFrameSent = now;

        try
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            byte[] pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height,
                PixelFormat.Rgba,
                PixelType.UnsignedByte, pixels);

            if (flipVertically)
            {
                pixels = FlipImageVertically(pixels, width, height);
            }

            var frameData = new FrameData
            {
                Width = width,
                Height = height,
                PixelData = pixels
            };

            return await SendFrameDataAsync(frameData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSender: Ошибка чтения framebuffer: {ex.Message}");
            OnError?.Invoke($"Ошибка чтения framebuffer: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Асинхронно отправляет данные кадра
    /// </summary>
    public async Task<bool> SendFrameDataAsync(FrameData frameData)
    {
        if (!IsClientConnected || _disposed || frameData == null)
            return false;

        try
        {
            var data = frameData.ToBytes();
            await _networkStream.WriteAsync(data, 0, data.Length);
            await _networkStream.FlushAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSender: Ошибка отправки: {ex.Message}");
            OnError?.Invoke($"Ошибка отправки: {ex.Message}");

            try
            {
                _networkStream?.Close();
                _connectedClient?.Close();
            }
            catch
            {
            }

            _networkStream = null;
            _connectedClient = null;

            return false;
        }
    }

    private byte[] FlipImageVertically(byte[] pixels, int width, int height)
    {
        byte[] flippedPixels = new byte[pixels.Length];
        int rowSize = width * 4; // RGBA = 4 байта на пиксель

        for (int y = 0; y < height; y++)
        {
            int sourceRow = (height - 1 - y) * rowSize;
            int destRow = y * rowSize;
            Array.Copy(pixels, sourceRow, flippedPixels, destRow, rowSize);
        }

        return flippedPixels;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
    }
}


public class FrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] PixelData { get; set; }

    public byte[] ToBytes()
    {
        var result = new byte[8 + PixelData.Length];
        BitConverter.GetBytes(Width).CopyTo(result, 0);
        BitConverter.GetBytes(Height).CopyTo(result, 4);
        PixelData.CopyTo(result, 8);
        return result;
    }

    public static FrameData FromBytes(byte[] data)
    {
        return new FrameData
        {
            Width = BitConverter.ToInt32(data, 0),
            Height = BitConverter.ToInt32(data, 4),
            PixelData = new ArraySegment<byte>(data, 8, data.Length - 8).ToArray()
        };
    }
}
