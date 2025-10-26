using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Utils
{
    public static class MMFShared
    {
        public const int MaxInputEvents = 32;

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
            public int Type;
            public int KeyCode;
            public float MouseX;
            public float MouseY;
            public int MouseButton;
            public float WheelDelta;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Header
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

        public unsafe struct StreamContext
        {
            public MemoryMappedViewAccessor Accessor;
            public byte* BasePtr;
            public byte* SlotsBase;
            public byte* InputEventsBase;
        }
        
        public class FrameData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] PixelData { get; set; }
        }
    }
}