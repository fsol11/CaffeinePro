using System.Runtime.InteropServices;

namespace CaffeinePro.Classes;

/// <summary>
/// A rectangle in the process's device-pixel coordinate space - the same space
/// <see cref="ScreenInfo.VirtualScreenBounds"/> and the monitor rects returned by Win32 use.
/// </summary>
public readonly record struct PixelRect(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;

    public int Bottom => Top + Height;
}

/// <summary>
/// A single physical display, plus whether Windows considers it the main display (the one that gets
/// the primary taskbar and sits at the origin of the virtual desktop).
/// </summary>
/// <remarks>
/// This is a small stand-in for System.Windows.Forms.Screen: WPF itself only ever exposes the
/// primary monitor and the virtual desktop as a whole, and this project deliberately does not
/// reference WinForms.
/// </remarks>
public sealed class ScreenInfo
{
    private ScreenInfo(PixelRect bounds, PixelRect workArea, bool isPrimary)
    {
        Bounds = bounds;
        WorkArea = workArea;
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// The monitor's full bounds (not its work area), in device pixels.
    /// </summary>
    public PixelRect Bounds
    {
        get;
    }

    /// <summary>
    /// The part of the monitor not covered by the taskbar or docked toolbars, in device pixels.
    /// </summary>
    public PixelRect WorkArea
    {
        get;
    }

    /// <summary>
    /// True for the display Windows designates as the main display.
    /// </summary>
    public bool IsPrimary
    {
        get;
    }

    /// <summary>
    /// The bounding rectangle of every monitor combined.
    /// </summary>
    public static PixelRect VirtualScreenBounds =>
        new(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN));

    /// <summary>
    /// All currently connected displays. Never empty: if enumeration fails for any reason, a single
    /// screen covering the whole virtual desktop is returned so callers always have something to
    /// work with.
    /// </summary>
    public static IReadOnlyList<ScreenInfo> GetAll()
    {
        var screens = new List<ScreenInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr _, ref Rect _, IntPtr _) =>
        {
            var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };

            if (GetMonitorInfo(monitor, ref info))
            {
                screens.Add(new ScreenInfo(
                    ToPixelRect(info.rcMonitor),
                    ToPixelRect(info.rcWork),
                    (info.dwFlags & MONITORINFOF_PRIMARY) != 0));
            }

            return true;
        }, IntPtr.Zero);

        if (screens.Count == 0)
        {
            screens.Add(new ScreenInfo(VirtualScreenBounds, VirtualScreenBounds, true));
        }

        return screens;
    }

    /// <summary>
    /// The main display, falling back to the first enumerated one if Windows reports no primary.
    /// </summary>
    public static ScreenInfo GetMain(IReadOnlyList<ScreenInfo> screens)
        => screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];

    /// <summary>
    /// The display containing the given device-pixel point, falling back to the main display.
    /// </summary>
    public static ScreenInfo FromPoint(int x, int y)
    {
        var screens = GetAll();
        return screens.FirstOrDefault(s => x >= s.Bounds.Left && x < s.Bounds.Right && y >= s.Bounds.Top && y < s.Bounds.Bottom)
               ?? GetMain(screens);
    }

    private static PixelRect ToPixelRect(Rect rect) => new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const uint MONITORINFOF_PRIMARY = 1;

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref Rect clip, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
