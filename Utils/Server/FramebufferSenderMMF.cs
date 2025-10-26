using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL.Compatibility;
using Utils;
using Buffer = System.Buffer;

public class FramebufferSenderMMF : IDisposable
{
    private FileStream _fileStream;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;
    private MMFShared.StreamContext _context;
    private byte[] _frameBuffer;
    private int _width, _height;
    private int _stride;
    private int _slotCount = 3;
    private volatile bool _disposed;
    private long _currentFrameId;

    private Thread _inputThread;
    private volatile bool _inputThreadRunning;

    public event Action OnClientConnected;
    public event Action OnClientDisconnected;
    public event Action<string> OnError;
    public event Action<MMFShared.InputEvent> OnInputEventReceived;

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
            long headerSize = Marshal.SizeOf<MMFShared.Header>();
            long slotSize = (long)_stride * _height;
            long inputEventsSize = Marshal.SizeOf<MMFShared.InputEvent>() * MMFShared.MaxInputEvents;
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

            var header = new MMFShared.Header
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

            _context = new MMFShared.StreamContext
            {
                Accessor = _accessor,
                BasePtr = basePtr,
                SlotsBase = basePtr + headerSize,
                InputEventsBase = basePtr + headerSize + _slotCount * slotSize
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
        catch (Exception ex)
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
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, _frameBuffer);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            if (flipVertically)
                FlipImageVertically(_frameBuffer, width, height);

            PublishFrame(_frameBuffer);

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private unsafe void PublishFrame(byte[] frameData)
    {
        if (_disposed || _context.BasePtr == null) return;

        _context.Accessor.Read(0, out MMFShared.Header header);

        int writeIndex = header.WriteIndex;
        long slotSize = (long)_stride * _height;
        byte* destination = _context.SlotsBase + writeIndex * slotSize;

        fixed (byte* source = frameData)
        {
            Buffer.MemoryCopy(source, destination, slotSize, frameData.LongLength);
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
            catch
            {
                // ignored
            }
        }
    }

    private unsafe void ProcessInputEvents()
    {
        if (_disposed || _context.BasePtr == null) return;

        _context.Accessor.Read(0, out MMFShared.Header header);

        while (header.InputReadIndex != header.InputWriteIndex)
        {
            int index = header.InputReadIndex % MMFShared.MaxInputEvents;
            long offset = index * Marshal.SizeOf<MMFShared.InputEvent>();

            byte* eventPtr = _context.InputEventsBase + offset;
            MMFShared.InputEvent inputEvent = Marshal.PtrToStructure<MMFShared.InputEvent>((IntPtr)eventPtr);

            OnInputEventReceived?.Invoke(inputEvent);

            header.InputReadIndex++;
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