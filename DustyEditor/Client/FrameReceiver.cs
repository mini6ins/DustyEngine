using System.Collections.Concurrent;
using System.Net.Sockets;

public class FrameReceiver : IDisposable
{
    private TcpClient _tcpClient;
    private NetworkStream _networkStream;
    private volatile bool _isConnected = false;
    private volatile bool _disposed = false;

    private readonly string _serverIP;
    private readonly int _serverPort;

    // Буферизация кадров
    private FrameData? _latestFrame;
    private readonly object _frameLock = new object();
    
    // Счетчики для диагностики
    private long _framesReceived = 0;
    private long _framesDropped = 0;
    private DateTime _lastStatsTime = DateTime.Now;

    // События
    public event Action OnConnected;
    public event Action<string> OnConnectionLost;
    public event Action<FrameData> OnFrameReceived;

    public bool IsConnected => _isConnected && !_disposed;

    public FrameReceiver(string serverIP = "127.0.0.1", int serverPort = 8080)
    {
        _serverIP = serverIP;
        _serverPort = serverPort;
    }

    public async Task<bool> ConnectAsync()
    {
        if (_disposed || _isConnected)
            return false;

        try
        {
            _tcpClient = new TcpClient();
            _tcpClient.ReceiveTimeout = 30000; // 30 секунд
            _tcpClient.SendTimeout = 30000;
            _tcpClient.NoDelay = true; // Отключаем буферизацию Nagle

            Console.WriteLine($"Подключение к {_serverIP}:{_serverPort}...");
            await _tcpClient.ConnectAsync(_serverIP, _serverPort);
            
            _networkStream = _tcpClient.GetStream();
            _isConnected = true;

            Console.WriteLine("Успешно подключен к серверу!");
            OnConnected?.Invoke();

            // Запускаем прослушивание в отдельной задаче
            _ = Task.Run(ListenForFrames);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения: {ex.Message}");
            OnConnectionLost?.Invoke($"Ошибка подключения: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;

        try
        {
            _networkStream?.Close();
            _tcpClient?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отключении: {ex.Message}");
        }
    }

    private async Task ListenForFrames()
    {
        Console.WriteLine("Запуск прослушивания кадров...");
        
        while (_isConnected && !_disposed && _tcpClient?.Connected == true)
        {
            try
            {
                // Читаем заголовок (8 байт: width + height)
                byte[] headerBuffer = new byte[8];
                if (!await ReadExactly(headerBuffer, 8))
                    break;

                int width = BitConverter.ToInt32(headerBuffer, 0);
                int height = BitConverter.ToInt32(headerBuffer, 4);
                int pixelDataSize = width * height * 4; // RGBA

                // Проверяем разумность размеров
                if (width <= 0 || height <= 0 || width > 8192 || height > 8192)
                {
                    Console.WriteLine($"Некорректные размеры кадра: {width}x{height}");
                    break;
                }

                // Читаем данные пикселей
                byte[] pixelBuffer = new byte[pixelDataSize];
                if (!await ReadExactly(pixelBuffer, pixelDataSize))
                    break;

                var frameData = new FrameData
                {
                    Width = width,
                    Height = height,
                    PixelData = pixelBuffer
                };

                // Сохраняем только последний кадр (no-drop strategy)
                lock (_frameLock)
                {
                    _latestFrame = frameData;
                }

                _framesReceived++;
                OnFrameReceived?.Invoke(frameData);

                // Периодически выводим статистику
                var now = DateTime.Now;
                if ((now - _lastStatsTime).TotalSeconds >= 5)
                {
                    Console.WriteLine($"Получено кадров: {_framesReceived}, пропущено: {_framesDropped}");
                    _lastStatsTime = now;
                }
            }
            catch (Exception ex)
            {
                if (_isConnected && !_disposed)
                {
                    Console.WriteLine($"Ошибка получения фрейма: {ex.Message}");
                    OnConnectionLost?.Invoke($"Ошибка получения фрейма: {ex.Message}");
                }
                break;
            }
        }

        _isConnected = false;
        if (!_disposed)
        {
            Console.WriteLine("Соединение с сервером потеряно");
            OnConnectionLost?.Invoke("Соединение потеряно");
        }
    }

    private async Task<bool> ReadExactly(byte[] buffer, int count)
    {
        int totalRead = 0;
        while (totalRead < count && !_disposed)
        {
            try
            {
                int read = await _networkStream.ReadAsync(buffer, totalRead, count - totalRead);
                if (read == 0)
                {
                    Console.WriteLine("Сервер закрыл соединение");
                    return false;
                }
                totalRead += read;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения данных: {ex.Message}");
                return false;
            }
        }

        return totalRead == count && !_disposed;
    }

    public bool TryDequeueFrame(out FrameData frameData)
    {
        lock (_frameLock)
        {
            frameData = _latestFrame;
            if (_latestFrame != null)
            {
                _latestFrame = null; // Очищаем после извлечения
                return true;
            }
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Disconnect();
        
        Console.WriteLine($"FrameReceiver закрыт. Всего получено кадров: {_framesReceived}");
    }
}