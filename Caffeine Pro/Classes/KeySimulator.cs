using System.Runtime.InteropServices;

namespace Caffeine_Pro.Classes;

public class KeySimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    struct Input
    {
        public uint type;
        public InputUnion U;
        public static int Size => Marshal.SizeOf(typeof(Input));
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput mi;
        [FieldOffset(0)]
        public KeyboardInput ki;
        [FieldOffset(0)]
        public HardwareInput hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MouseInput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KeyboardInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HardwareInput
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // ReSharper disable once InconsistentNaming
    const uint INPUT_KEYBOARD = 1;
    // ReSharper disable once IdentifierTypo
    // ReSharper disable once InconsistentNaming
    const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const uint KEYEVENTF_KEYUP = 0x0002;
    // ReSharper disable once InconsistentNaming
    const uint VK_F15 = 0x7E;

    public static void PressF15()
    {
        var inputs = new Input[2];

        // Press the F15 key
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = (ushort)VK_F15;

        // Release the F15 key
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].U.ki.wVk = (ushort)VK_F15;
        inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;

        SendInput((uint)inputs.Length, inputs, Input.Size);
    }
}
