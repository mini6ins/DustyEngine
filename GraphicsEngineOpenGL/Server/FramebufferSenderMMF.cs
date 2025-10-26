using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL.Compatibility;
using Buffer = System.Buffer;

public class FramebufferSenderMMF : IDisposable
{
    private const int MaxInputEvents = 32;

    public enum InputEventType
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
        public long SlotSize;
    }

    private FileStream _fileStream;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;
    private StreamContext _context;
    private byte[] _frameBuffer;
    private int _width;
    private int _height;
    private int _stride;
    private int _slotCount = 3;
    private volatile bool _disposed;
    private long _currentFrameId;

    private Thread _inputThread;
    private volatile bool _inputThreadRunning;

    public event Action OnClientConnected;
    public event Action OnClientDisconnected;
    public event Action<string> OnError;
    public event Action<InputEvent> OnInputEventReceived;

    public bool IsRunning => !_disposed;

    public FramebufferSenderMMF(int width = 1280, int height = 720, int targetFps = 60)
    {
        _width = width;
        _height = height;
        _stride = width * 4;
        _frameBuffer = new byte[_stride * _height];
    }

    public unsafe bool Start()
    {
        if (_disposed || _mmf != null)
            return false;

        try
        {
            long headerSize = Marshal.SizeOf<Header>();
            long slotSize = (long)_stride * _height;
            long inputEventsSize = Marshal.SizeOf<InputEvent>() * MaxInputEvents;
            long totalSize = headerSize + _slotCount * slotSize + inputEventsSize;

            string path = Directory.Exists("/dev/shm")
                ? "/dev/shm/vid_stream.mmf"
                : Path.Combine(Path.GetTempPath(), "vid_stream.mmf");

            _fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (_fileStream.Length != totalSize)
            {
                _fileStream.SetLength(totalSize);
                _fileStream.Flush(true);
            }

            _mmf = MemoryMappedFile.CreateFromFile(_fileStream, null, totalSize,
                MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, false);
            _accessor = _mmf.CreateViewAccessor(0, totalSize, MemoryMappedFileAccess.ReadWrite);

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

            byte* basePtr = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

            _context = new StreamContext
            {
                Accessor = _accessor,
                BasePtr = basePtr,
                SlotsBase = basePtr + headerSize,
                InputEventsBase = basePtr + headerSize + _slotCount * slotSize,
                SlotSize = slotSize,
            };

            _inputThreadRunning = true;
            _inputThread = new Thread(InputProcessingLoop)
            {
                IsBackground = true,
                Name = "InputProcessor",
                Priority = ThreadPriority.Highest
            };
            _inputThread.Start();

            OnClientConnected?.Invoke();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void Stop()
    {
        if (_disposed) return;

        try
        {
            _inputThreadRunning = false;

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
        }
        catch
        {
            // ignored
        }
    }


    public bool SendFramebuffer(int framebufferId, int width, int height, bool flipVertically = true)
    {
        if (_disposed) return false;

        try
        {
            int need = _stride * _height;
            if (_frameBuffer.Length != need)
                _frameBuffer = new byte[need];

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebufferId);
            GL.ReadPixels(0, 0, width, height,
                PixelFormat.Rgba,
                PixelType.UnsignedByte, _frameBuffer);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            if (flipVertically)
                FlipImageVertically(_frameBuffer, width, height);

            PublishFrame(_frameBuffer);

            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Error sending framebuffer: {ex.Message}");
            return false;
        }
    }

    private unsafe void PublishFrame(byte[] frameData)
    {
        if (_disposed || _context.BasePtr == null) return;

        _context.Accessor.Read(0, out Header header);

        int writeIndex = header.WriteIndex;
        byte* destination = _context.SlotsBase + writeIndex * _context.SlotSize;

        fixed (byte* source = frameData)
        {
            Buffer.MemoryCopy(source, destination, _context.SlotSize, frameData.LongLength);
        }

        header.FrameId = ++_currentFrameId;
        header.WriteIndex = (writeIndex + 1) % header.SlotCount;
        _context.Accessor.Write(0, ref header);
    }

    private void InputProcessingLoop()
    {
        while (_inputThreadRunning && !_disposed)
        {
            try
            {
                ProcessInputEvents();
                Thread.Sleep(1);
            }
            catch (Exception ex)
            {
                if (_inputThreadRunning && !_disposed)
                {
                }
            }
        }
    }

    private unsafe void ProcessInputEvents()
    {
        if (_disposed || _context.BasePtr == null) return;

        _context.Accessor.Read(0, out Header header);

        while (header.InputReadIndex != header.InputWriteIndex)
        {
            int index = header.InputReadIndex % MaxInputEvents;
            long offset = index * Marshal.SizeOf<InputEvent>();

            byte* eventPtr = _context.InputEventsBase + offset;
            InputEvent inputEvent = Marshal.PtrToStructure<InputEvent>((IntPtr)eventPtr);

            OnInputEventReceived?.Invoke(inputEvent);

            header.InputReadIndex = (header.InputReadIndex + 1);
            _context.Accessor.Write(0, ref header);
            _context.Accessor.Read(0, out header);
        }
    }


    private void FlipImageVertically(byte[] pixels, int width, int height)
    {
        int rowSize = width * 4;
        byte[] temp = new byte[rowSize];

        for (int y = 0; y < height / 2; y++)
        {
            int topRow = y * rowSize;
            int bottomRow = (height - 1 - y) * rowSize;

            Array.Copy(pixels, topRow, temp, 0, rowSize);
            Array.Copy(pixels, bottomRow, pixels, topRow, rowSize);
            Array.Copy(temp, 0, pixels, bottomRow, rowSize);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}