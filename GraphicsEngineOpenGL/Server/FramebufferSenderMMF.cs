using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL.Compatibility;
using Buffer = System.Buffer;

public class FramebufferSenderMMF : IDisposable
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
        public InputEventType Type;
        public int KeyCode;
        public float MouseX;
        public float MouseY;
        public int MouseButton;
        public float WheelDelta;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct Header
    {
        public int Width;
        public int Height;
        public int Stride;
        public int SlotCount;
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
    }

    private FileStream _fileStream;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;
    private unsafe StreamContext _context;
    private byte[] _frameBuffer;
    private int _width;
    private int _height;
    private int _stride;
    private int _slotCount = 3;
    private volatile bool _disposed = false;
    private DateTime _lastFrameSent = DateTime.MinValue;
    private readonly TimeSpan _frameInterval;
    private long _currentFrameId = 0;

    public event Action OnClientConnected;
    public event Action OnClientDisconnected;
    public event Action<string> OnError;
    public event Action<InputEvent> OnInputEventReceived;

    public bool IsRunning => !_disposed && _mmf != null;

    public FramebufferSenderMMF(int width = 1920, int height = 1080, int targetFPS = 30)
    {
        _width = width;
        _height = height;
        _stride = width * 4; // RGBA
        _frameBuffer = new byte[_stride * _height];
        _frameInterval = TimeSpan.FromMilliseconds(1000.0 / targetFPS);
    }

    public unsafe bool Start()
    {
        if (_disposed || _mmf != null)
            return false;

        try
        {
            long headerSize = Marshal.SizeOf<Header>();
            long slotSize = (long)_stride * _height;
            long inputEventsSize = Marshal.SizeOf<InputEvent>() * MAX_INPUT_EVENTS;
            long totalSize = headerSize + _slotCount * slotSize + inputEventsSize;

            // ВАЖНО: используем тот же путь, что и Consumer!
            string path = Directory.Exists("/dev/shm")
                ? "/dev/shm/vid_stream.mmf"
                : Path.Combine(Path.GetTempPath(), "vid_stream.mmf");

            Console.WriteLine($"FramebufferSenderMMF: Creating MMF at {path}");

            _fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (_fileStream.Length != totalSize)
            {
                _fileStream.SetLength(totalSize);
                _fileStream.Flush(true);
            }

            _mmf = MemoryMappedFile.CreateFromFile(_fileStream, null, totalSize,
                MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, false);
            _accessor = _mmf.CreateViewAccessor(0, totalSize, MemoryMappedFileAccess.ReadWrite);

            // Инициализируем заголовок
            var header = new Header
            {
                Width = _width,
                Height = _height,
                Stride = _stride,
                SlotCount = _slotCount,
                WriteIndex = 0,
                FrameId = 0,
                InputWriteIndex = 0,
                InputReadIndex = 0
            };
            _accessor.Write(0, ref header);

            // Получаем указатели для прямого доступа
            byte* basePtr = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

            _context = new StreamContext
            {
                Accessor = _accessor,
                BasePtr = basePtr,
                SlotsBase = basePtr + headerSize,
                InputEventsBase = basePtr + headerSize + _slotCount * slotSize,
                HeaderSize = headerSize,
                SlotSize = slotSize,
                InputEventsSize = inputEventsSize
            };

            Console.WriteLine($"FramebufferSenderMMF: Started successfully");
            Console.WriteLine($"  Resolution: {_width}x{_height}");
            Console.WriteLine($"  Slots: {_slotCount}");
            Console.WriteLine($"  Total size: {totalSize / 1024 / 1024:F2} MB");

            OnClientConnected?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSenderMMF: Error starting: {ex.Message}");
            OnError?.Invoke($"Error starting: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        if (_disposed) return;

        try
        {
            unsafe
            {
                if (_context.BasePtr != null)
                {
                    _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
                    _context.BasePtr = null;
                }
            }

            _accessor?.Dispose();
            _mmf?.Dispose();
            _fileStream?.Dispose();

            OnClientDisconnected?.Invoke();
            Console.WriteLine("FramebufferSenderMMF: Stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSenderMMF: Error stopping: {ex.Message}");
        }
    }

    public bool SendFramebuffer(int framebufferId, int width, int height, bool flipVertically = true)
    {
        if (_disposed || _mmf == null)
            return false;

        // Ограничиваем частоту отправки
        var now = DateTime.Now;
        if (now - _lastFrameSent < _frameInterval)
            return true;

        _lastFrameSent = now;

        try
        {
            // Проверяем и обновляем размер если нужно
            if (width != _width || height != _height)
            {
                Console.WriteLine($"FramebufferSenderMMF: Resolution mismatch {width}x{height} vs {_width}x{_height}");
                // Для простоты пока игнорируем ресайз
                // ResizeBuffers(width, height);
            }

            // Читаем пиксели из framebuffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            GL.ReadPixels(0, 0, _width, _height,
                PixelFormat.Rgba,
                PixelType.UnsignedByte, _frameBuffer);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // Переворачиваем изображение если нужно
            if (flipVertically)
            {
                FlipImageVertically(_frameBuffer, _width, _height);
            }

            // Публикуем кадр через MMF
            PublishFrame(_frameBuffer);

            // Обрабатываем входящие события
            ProcessInputEvents();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FramebufferSenderMMF: Error sending framebuffer: {ex.Message}");
            OnError?.Invoke($"Error sending framebuffer: {ex.Message}");
            return false;
        }
    }

    private unsafe void PublishFrame(byte[] frameData)
    {
        if (_disposed || _context.BasePtr == null) return;

        Header header;
        _context.Accessor.Read(0, out header);

        int writeIndex = header.WriteIndex;
        byte* destination = _context.SlotsBase + (long)writeIndex * _context.SlotSize;

        // Копируем данные кадра
        fixed (byte* source = frameData)
        {
            Buffer.MemoryCopy(source, destination, _context.SlotSize, frameData.LongLength);
        }

        // Обновляем заголовок
        header.FrameId = ++_currentFrameId;
        header.WriteIndex = (writeIndex + 1) % header.SlotCount;
        _context.Accessor.Write(0, ref header);
    }

    private unsafe void ProcessInputEvents()
    {
        if (_disposed || _context.BasePtr == null) return;

        Header header;
        _context.Accessor.Read(0, out header);

        while (header.InputReadIndex != header.InputWriteIndex)
        {
            int index = header.InputReadIndex % MAX_INPUT_EVENTS;
            long offset = index * Marshal.SizeOf<InputEvent>();

            // Читаем событие
            byte* eventPtr = _context.InputEventsBase + offset;
            InputEvent inputEvent = Marshal.PtrToStructure<InputEvent>((IntPtr)eventPtr);

            // Вызываем обработчик события
            OnInputEventReceived?.Invoke(inputEvent);

            // Обновляем индекс чтения
            header.InputReadIndex = (header.InputReadIndex + 1);
            _context.Accessor.Write(0, ref header);
            _context.Accessor.Read(0, out header);
        }
    }

    public unsafe void SendInputEvent(InputEvent inputEvent)
    {
        if (_disposed || _context.BasePtr == null) return;

        Header header;
        _context.Accessor.Read(0, out header);

        int writeIndex = header.InputWriteIndex % MAX_INPUT_EVENTS;
        long offset = writeIndex * Marshal.SizeOf<InputEvent>();

        // Записываем событие
        byte* eventPtr = _context.InputEventsBase + offset;
        Marshal.StructureToPtr(inputEvent, (IntPtr)eventPtr, false);

        // Обновляем индекс записи
        header.InputWriteIndex = (header.InputWriteIndex + 1);
        _context.Accessor.Write(0, ref header);
    }

    private void FlipImageVertically(byte[] pixels, int width, int height)
    {
        int rowSize = width * 4; // RGBA
        byte[] temp = new byte[rowSize];

        for (int y = 0; y < height / 2; y++)
        {
            int topRow = y * rowSize;
            int bottomRow = (height - 1 - y) * rowSize;

            // Swap rows
            Array.Copy(pixels, topRow, temp, 0, rowSize);
            Array.Copy(pixels, bottomRow, pixels, topRow, rowSize);
            Array.Copy(temp, 0, pixels, bottomRow, rowSize);
        }
    }

    public (int width, int height) GetResolution()
    {
        return (_width, _height);
    }

    public long GetCurrentFrameId()
    {
        return _currentFrameId;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Stop();
    }
}