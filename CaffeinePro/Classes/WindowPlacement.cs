using System.Runtime.InteropServices;

namespace CaffeinePro.Classes;

/// <summary>
/// Win32 helpers shared by the full-screen overlays (the afterwards-action warning and the blackout
/// screen): making a window cover exactly one monitor, and taking the foreground from whatever
/// application currently owns it.
/// </summary>
public static class WindowPlacement
{
    /// <summary>
    /// Sizes a window to cover exactly one monitor.
    /// </summary>
    /// <remarks>
    /// Done with SetWindowPos on the raw monitor rectangle rather than by assigning WPF's
    /// Left/Top/Width/Height: those are device-independent units, so covering a monitor exactly
    /// would mean reproducing Windows' own pixel-to-DIP rounding for that monitor's scaling, and
    /// getting it slightly wrong leaves the window straddling a screen edge. Windows reports the
    /// rectangle in pixels, so it is used in pixels. WPF picks the new rectangle up from
    /// WM_WINDOWPOSCHANGED and keeps Left/Top/ActualWidth/ActualHeight in sync by itself.
    /// </remarks>
    public static void CoverScreen(IntPtr hwnd, PixelRect bounds)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(hwnd, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>
    /// Pulls a window to the foreground and puts the keyboard focus on it.
    /// </summary>
    /// <remarks>
    /// These overlays are raised from a background timer or a global hotkey while another
    /// application owns the foreground, and Windows then quietly ignores a plain
    /// SetForegroundWindow: only the process owning the foreground window may hand it over. Two
    /// things lift that restriction, and both are used here because either can fail on its own -
    /// briefly attaching this thread's input queue to the foreground window's thread, and zeroing
    /// the foreground lock timeout for the duration of the call (restored immediately afterwards).
    /// Without this the overlay is visible but deaf, and Escape - the only way out - does nothing.
    /// </remarks>
    public static void TakeForeground(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        var currentThread = GetCurrentThreadId();
        var attached = foregroundThread != 0
                       && foregroundThread != currentThread
                       && AttachThreadInput(currentThread, foregroundThread, true);

        uint previousTimeout = 0;
        var timeoutOverridden = SystemParametersInfoGet(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref previousTimeout, 0)
                                && SystemParametersInfoSet(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, SPIF_SENDCHANGE);

        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);
        SetActiveWindow(hwnd);
        SetFocus(hwnd);

        if (timeoutOverridden)
        {
            SystemParametersInfoSet(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, previousTimeout, SPIF_SENDCHANGE);
        }

        if (attached)
        {
            AttachThreadInput(currentThread, foregroundThread, false);
        }
    }

    /// <summary>
    /// Keeps a window out of the taskbar and the Alt+Tab list.
    /// </summary>
    /// <remarks>
    /// Used instead of WPF's ShowInTaskbar="False", which implements the same thing by parenting
    /// the window to a hidden owner window it creates and destroys behind the scenes - and when
    /// that owner goes away it takes these overlays with it, leaving the application convinced they
    /// are still open. The WS_EX_TOOLWINDOW style has no such side effect. Must be applied before
    /// the window is first shown.
    /// </remarks>
    public static void HideFromTaskbar(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(exStyle.ToInt64() | WS_EX_TOOLWINDOW));
    }

    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
    private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
    private const uint SPIF_SENDCHANGE = 0x0002;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attachTo, uint attachFrom, bool attach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static extern bool SystemParametersInfoGet(uint action, uint param, ref uint value, uint winIni);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static extern bool SystemParametersInfoSet(uint action, uint param, IntPtr value, uint winIni);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static extern bool SystemParametersInfoSet(uint action, uint param, uint value, uint winIni);
}
