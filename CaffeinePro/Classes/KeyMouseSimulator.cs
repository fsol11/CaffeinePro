using System.Runtime.InteropServices;

namespace CaffeinePro.Classes;

/// <summary>
/// This class implements a method to simulate a key press
/// </summary>
internal static class KeyMouseSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public InputUnion U;
        public static int Size => Marshal.SizeOf(typeof(Input));
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput mi;
        [FieldOffset(0)]
        public KeyboardInput ki;
        [FieldOffset(0)]
        public HardwareInput hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // ReSharper disable once InconsistentNaming
    private const uint INPUT_KEYBOARD = 1;
    // ReSharper disable once InconsistentNaming
    private const uint INPUT_MOUSE = 0;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const uint KEYEVENTF_KEYUP = 0x0002;
    // ReSharper disable once InconsistentNaming
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    // ReSharper disable once InconsistentNaming
    private const uint VK_F14 = 0x7D;
    // ReSharper disable once InconsistentNaming
    private const uint VK_F15 = 0x7E;

    private static readonly Random _random = new();

    private static void PressKey(ushort vk)
    {
        var inputs = new Input[2];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = vk;
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].U.ki.wVk = vk;
        inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;
        SendInput((uint)inputs.Length, inputs, Input.Size);
    }

    public static void PressF14() => PressKey((ushort)VK_F14);

    public static void PressF15() => PressKey((ushort)VK_F15);

    public static void MoveMouseSquare()
    {
        // Four directions as (dx, dy): right, down, left, up
        (int dx, int dy)[] directions = [(1, 0), (0, 1), (-1, 0), (0, -1)];

        // Shuffle starting direction randomly
        var start = _random.Next(4);

        var inputs = new Input[4];
        for (var i = 0; i < 4; i++)
        {
            var (dx, dy) = directions[(start + i) % 4];
            inputs[i].type = INPUT_MOUSE;
            inputs[i].U.mi.dx = dx;
            inputs[i].U.mi.dy = dy;
            inputs[i].U.mi.dwFlags = MOUSEEVENTF_MOVE;
        }

        SendInput((uint)inputs.Length, inputs, Input.Size);
    }

    public static void SendKeepAwakeSignal()
    {
        switch (_random.Next(3))
        {
            case 0:
                PressF14();
                break;
            case 1:
                PressF15();
                break;
            default:
                MoveMouseSquare();
                break;
        }
    }
}
