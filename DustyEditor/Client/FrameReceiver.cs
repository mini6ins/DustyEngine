using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

public class FrameReceiver : IDisposable
{
    private const int MAX_INPUT_EVENTS = 32;

    public enum InputEventType : int
    {
        None = 0,
        KeyDown = 1,
        KeyUp = 2,
        MouseMove = 3,
        MouseDown = 4,
        MouseUp = 5,
        MouseWheel = 6
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct InputEvent
    {
        public int Type;
        public int KeyCode;
        public float MouseX;
        public float MouseY;
        public int MouseButton;
        public float WheelDelta;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct Header
    {
        public int Width, Height, Stride, SlotCount;
        public volatile int WriteIndex;
        public long FrameId;
        public volatile int InputWriteIndex;
        public volatile int InputReadIndex;
    }

    private unsafe struct StreamContext
    {
        public MemoryMappedViewAccessor Accessor;
        public byte* BasePtr;
        public byte* SlotsBase;
        public byte* InputEventsBase;
        public long HeaderSize;
        public long SlotSize;
        public long InputEventsSize;
        public int Width;
        public int Height;
        public int Stride;
        public int SlotCount;
    }

    private StreamContext _ctx;
    private long _lastFrame = -1;
    private volatile bool _isConnected = false;
    private volatile bool _disposed = false;

    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _acc;
    private FileStream _fs;
    private Thread _readThread;

    private readonly string _mmfPath;

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

    public FrameReceiver(string mmfPath = null)
    {
        // Используем тот же путь, что и в Producer/Sender
        _mmfPath = mmfPath ?? (Directory.Exists("/dev/shm")
            ? "/dev/shm/vid_stream.mmf"
            : Path.Combine(Path.GetTempPath(), "vid_stream.mmf"));
    }

    public async Task<bool> ConnectAsync()
    {
        if (_disposed || _isConnected)
            return false;

        try
        {
            Console.WriteLine($"Ожидание MMF файла: {_mmfPath}...");

            // Ждём появления файла
            if (!await WaitForFileAsync(_mmfPath, TimeSpan.FromSeconds(10)))
            {
                Console.WriteLine("MMF файл не найден в течение таймаута");
                return false;
            }

            await Task.Run(() => InitializeSharedMemory());

            _isConnected = true;
            Console.WriteLine($"Успешно подключен к MMF! Разрешение: {_ctx.Width}x{_ctx.Height}");
            OnConnected?.Invoke();

            // Запускаем прослушивание в отдельном потоке
            _readThread = new Thread(ListenForFrames) { IsBackground = true };
            _readThread.Start();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения к MMF: {ex.Message}");
            OnConnectionLost?.Invoke($"Ошибка подключения: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> WaitForFileAsync(string path, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!File.Exists(path))
        {
            if ((DateTime.UtcNow - start) > timeout)
                return false;
            await Task.Delay(100);
        }

        return true;
    }

    private unsafe void InitializeSharedMemory()
    {
        _fs = new FileStream(_mmfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        _mmf = MemoryMappedFile.CreateFromFile(_fs, null, 0, MemoryMappedFileAccess.ReadWrite,
            HandleInheritability.Inheritable, false);
        _acc = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);

        Header hdr;
        _acc.Read(0, out hdr);
        long headerSize = Marshal.SizeOf<Header>();
        long slotSize = (long)hdr.Stride * hdr.Height;
        long inputEventsSize = Marshal.SizeOf<InputEvent>() * MAX_INPUT_EVENTS;

        byte* basePtr = null;
        _acc.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

        _ctx = new StreamContext
        {
            Accessor = _acc,
            BasePtr = basePtr,
            SlotsBase = basePtr + headerSize,
            InputEventsBase = basePtr + headerSize + hdr.SlotCount * slotSize,
            HeaderSize = headerSize,
            SlotSize = slotSize,
            InputEventsSize = inputEventsSize,
            Width = hdr.Width,
            Height = hdr.Height,
            Stride = hdr.Stride,
            SlotCount = hdr.SlotCount
        };

        Console.WriteLine($"MMF инициализирован: {hdr.Width}x{hdr.Height}, слотов: {hdr.SlotCount}");
    }

    public void Disconnect()
    {
        _isConnected = false;
    }

    private unsafe void ListenForFrames()
    {
        Console.WriteLine("Запуск прослушивания кадров из MMF...");

        while (_isConnected && !_disposed)
        {
            try
            {
                var frame = TryReadFrame();

                if (frame != null)
                {
                    var frameData = new FrameData
                    {
                        Width = frame.Value.Width,
                        Height = frame.Value.Height,
                        PixelData = frame.Value.PixelData
                    };

                    // Сохраняем только последний кадр
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
                else
                {
                    // Нет новых кадров, небольшая пауза
                    Thread.Sleep(1);
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
            Console.WriteLine("Соединение с MMF потеряно");
            OnConnectionLost?.Invoke("Соединение потеряно");
        }
    }

    private unsafe (int Width, int Height, byte[] PixelData)? TryReadFrame()
    {
        Header hdr;
        _ctx.Accessor.Read(0, out hdr);
        long fid = hdr.FrameId;

        if (fid == _lastFrame)
        {
            return null;
        }

        int ready = (hdr.WriteIndex - 1 + hdr.SlotCount) % hdr.SlotCount;
        byte* framePtr = _ctx.SlotsBase + (long)ready * _ctx.SlotSize;

        _lastFrame = fid;

        // Копируем данные в управляемый массив
        byte[] pixelData = new byte[_ctx.Stride * _ctx.Height];
        Marshal.Copy((IntPtr)framePtr, pixelData, 0, pixelData.Length);

        return (_ctx.Width, _ctx.Height, pixelData);
    }

    public bool TryDequeueFrame(out FrameData frameData)
    {
        lock (_frameLock)
        {
            frameData = _latestFrame;
            if (_latestFrame != null)
            {
                _latestFrame = null;
                return true;
            }
        }

        return false;
    }

    public unsafe void SendInputEvent(InputEvent inputEvent)
    {
        if (!_isConnected || _disposed || _ctx.BasePtr == null) return;

        try
        {
            Header hdr;
            _ctx.Accessor.Read(0, out hdr);

            int writeIndex = hdr.InputWriteIndex % MAX_INPUT_EVENTS;
            long offset = writeIndex * Marshal.SizeOf<InputEvent>();

            byte* eventPtr = _ctx.InputEventsBase + offset;
            Marshal.StructureToPtr(inputEvent, (IntPtr)eventPtr, false);

            hdr.InputWriteIndex = (hdr.InputWriteIndex + 1);
            _ctx.Accessor.Write(0, ref hdr);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENT] Error sending input event: {ex.Message}");
        }
    }

    public unsafe void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Disconnect();

        if (_ctx.BasePtr != null && _acc != null)
            _acc.SafeMemoryMappedViewHandle.ReleasePointer();

        _acc?.Dispose();
        _mmf?.Dispose();
        _fs?.Dispose();

        Console.WriteLine($"FrameReceiver закрыт. Всего получено кадров: {_framesReceived}");
    }
}