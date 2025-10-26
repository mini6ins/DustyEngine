using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Utils;

public class FrameReceiver : IDisposable
{
    private MMFShared.StreamContext _ctx;
    private int _width;
    private int _height;
    private int _stride;
    private long _lastFrame = -1;
    private volatile bool _isConnected;
    private volatile bool _disposed;

    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _acc;
    private FileStream _fs;
    private Thread _readThread;

    private readonly string _mmfPath;
    
    private long _framesDropped = 0;
    private DateTime _lastStatsTime = DateTime.Now;

    public event Action OnConnected;
    public event Action<string> OnConnectionLost;
    public event Action<MMFShared.FrameData> OnFrameReceived;

    public bool IsConnected => _isConnected && !_disposed;

    public FrameReceiver(string mmfPath = null)
    {
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
          

            if (!await WaitForFileAsync(_mmfPath, TimeSpan.FromSeconds(10)))
            {
    
                return false;
            }

            await Task.Run(InitializeSharedMemory);

            _isConnected = true;
            OnConnected?.Invoke();

            _readThread = new Thread(ListenForFrames) { IsBackground = true };
            _readThread.Start();

            return true;
        }
        catch (Exception ex)
        {
            OnConnectionLost?.Invoke($"Connection error: {ex.Message}");
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

        _acc.Read(0, out MMFShared.Header hdr);
        
        _width = hdr.Width;
        _height = hdr.Height;
        _stride = hdr.Stride;

        long headerSize = Marshal.SizeOf<MMFShared.Header>();
        long slotSize = (long)hdr.Stride * hdr.Height;

        byte* basePtr = null;
        _acc.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

        _ctx = new MMFShared.StreamContext
        {
            Accessor = _acc,
            BasePtr = basePtr,
            SlotsBase = basePtr + headerSize,
            InputEventsBase = basePtr + headerSize + hdr.SlotCount * slotSize
        };
    }

    public void Disconnect()
    {
        _isConnected = false;
    }

    private void ListenForFrames()
    {
        while (_isConnected && !_disposed)
        {
            try
            {
                var frame = TryReadFrame();

                if (frame != null)
                {
                    var frameData = new MMFShared.FrameData
                    {
                        Width = frame.Value.Width,
                        Height = frame.Value.Height,
                        PixelData = frame.Value.PixelData
                    };
                    
                    OnFrameReceived?.Invoke(frameData);

                    var now = DateTime.Now;
                    if ((now - _lastStatsTime).TotalSeconds >= 5)
                    {
                        _lastStatsTime = now;
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                if (_isConnected && !_disposed)
                {
                    OnConnectionLost?.Invoke($"Error: {ex.Message}");
                }
                break;
            }
        }

        _isConnected = false;
        if (!_disposed)
        {
            OnConnectionLost?.Invoke("Connection lost");
        }
    }

    private unsafe (int Width, int Height, byte[] PixelData)? TryReadFrame()
    {
        _ctx.Accessor.Read(0, out MMFShared.Header hdr);
        long fid = hdr.FrameId;

        if (fid == _lastFrame)
            return null;

        int ready = (hdr.WriteIndex - 1 + hdr.SlotCount) % hdr.SlotCount;
        byte* framePtr = _ctx.SlotsBase + (long)ready * _stride * _height;

        _lastFrame = fid;

        byte[] pixelData = new byte[_stride * _height];
        Marshal.Copy((IntPtr)framePtr, pixelData, 0, pixelData.Length);

        return (_width, _height, pixelData);
    }

    
    public unsafe void SendInputEvent(MMFShared.InputEvent inputEvent)
    {
        if (!_isConnected || _disposed || _ctx.BasePtr == null) 
            return;

        try
        {
            _ctx.Accessor.Read(0, out MMFShared.Header hdr);

            int writeIndex = hdr.InputWriteIndex % MMFShared.MaxInputEvents;
            long offset = writeIndex * Marshal.SizeOf<MMFShared.InputEvent>();

            byte* eventPtr = _ctx.InputEventsBase + offset;
            Marshal.StructureToPtr(inputEvent, (IntPtr)eventPtr, false);

            hdr.InputWriteIndex++;
            _ctx.Accessor.Write(0, ref hdr);
        }
        catch
        {
            // ignored
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
    }
}

