using System.Runtime.InteropServices;

namespace Caffeine_Pro.Classes;

/// <summary>
/// This class implements a method to simulate a mouse move
/// </summary>
public class MouseSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    struct INPUT
    {
        public uint type;
        public InputUnion U;
        public static int Size => Marshal.SizeOf(typeof(INPUT));
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // ReSharper disable once InconsistentNaming
    const uint INPUT_MOUSE = 0;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const uint MOUSEEVENTF_MOVE = 0x0001;

    public static void MoveMouse(int dx, int dy)
    {
        var inputs = new INPUT[1];

        // Move the mouse
        inputs[0].type = INPUT_MOUSE;
        inputs[0].U.mi.dx = dx;
        inputs[0].U.mi.dy = dy;
        inputs[0].U.mi.dwFlags = MOUSEEVENTF_MOVE;

        SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }
}
