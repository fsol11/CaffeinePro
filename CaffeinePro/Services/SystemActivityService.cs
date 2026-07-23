using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CaffeinePro.Services;

/// <summary>
/// Detects when the user returns to an idle machine, i.e. the computer becomes
/// active right after the display was turned off because of inactivity, or right
/// after a screen saver was running.
///
/// This complements <see cref="WindowsSessionService"/> (which only reports session
/// lock/unlock) by monitoring the two cases that do not raise a session switch:
///   * the monitor powering off/on (GUID_CONSOLE_DISPLAY_STATE via WM_POWERBROADCAST)
///   * a screen saver starting/stopping (polled via SPI_GETSCREENSAVERRUNNING)
/// </summary>
public sealed class SystemActivityService : IDisposable
{
    /// <summary>
    /// Raised when the computer becomes active again after the display was off
    /// or a screen saver was running due to inactivity.
    /// </summary>
    public event EventHandler? OnUserBecameActive;

    private HwndSource? _messageWindow;
    private IntPtr _displayNotificationHandle = IntPtr.Zero;
    private readonly DispatcherTimer _screenSaverTimer;

    private DisplayState _lastDisplayState = DisplayState.On;
    private bool _wasScreenSaverRunning;

    public SystemActivityService()
    {
        _messageWindow = CreateMessageOnlyWindow();
        _messageWindow.AddHook(WndProc);

        _displayNotificationHandle =
            RegisterPowerSettingNotification(_messageWindow.Handle,
                ref GuidConsoleDisplayState, DeviceNotifyWindowHandle);

        _screenSaverTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _screenSaverTimer.Tick += CheckScreenSaver;
        _screenSaverTimer.Start();
    }

    private enum DisplayState
    {
        Off = 0,
        On = 1,
        Dimmed = 2
    }

    // -- Display power state (screen blanked because of inactivity) ------------------------------

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmPowerBroadcast && wParam.ToInt32() == PbtPowerSettingChange)
        {
            var setting = Marshal.PtrToStructure<PowerBroadcastSetting>(lParam);
            if (setting.PowerSetting == GuidConsoleDisplayState)
            {
                OnDisplayStateChanged((DisplayState)setting.Data);
            }
        }

        return IntPtr.Zero;
    }

    private void OnDisplayStateChanged(DisplayState state)
    {
        // The display turning back on after being off means the user is back at the machine.
        if (state == DisplayState.On && _lastDisplayState == DisplayState.Off)
        {
            OnUserBecameActive?.Invoke(this, EventArgs.Empty);
        }

        _lastDisplayState = state;
    }

    // -- Screen saver ---------------------------------------------------------------------------

    private void CheckScreenSaver(object? sender, EventArgs e)
    {
        var running = false;
        _ = SystemParametersInfo(SpiGetScreenSaverRunning, 0, ref running, 0);

        // A running -> stopped transition means the user dismissed the screen saver.
        if (_wasScreenSaverRunning && !running)
        {
            OnUserBecameActive?.Invoke(this, EventArgs.Empty);
        }

        _wasScreenSaverRunning = running;
    }

    // -- Message-only window --------------------------------------------------------------------

    private static HwndSource CreateMessageOnlyWindow()
    {
        var parameters = new HwndSourceParameters("CaffeineProSystemActivityMonitor")
        {
            Width = 0,
            Height = 0,
            ParentWindow = HwndMessage
        };

        return new HwndSource(parameters);
    }

    // -- Native interop -------------------------------------------------------------------------

    private const int WmPowerBroadcast = 0x0218;
    private const int PbtPowerSettingChange = 0x8013;
    private const int DeviceNotifyWindowHandle = 0x00000000;
    private const uint SpiGetScreenSaverRunning = 0x0072;
    private static readonly IntPtr HwndMessage = new(-3);

    // ReSharper disable once InconsistentNaming
    private static Guid GuidConsoleDisplayState = new("6fe69556-704a-47a0-8f24-c28d936fda47");

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PowerBroadcastSetting
    {
        public Guid PowerSetting;
        public uint DataLength;
        public byte Data;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient,
        ref Guid powerSettingGuid, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        ref bool pvParam, uint fWinIni);

    // -- Cleanup --------------------------------------------------------------------------------

    public void Dispose()
    {
        _screenSaverTimer.Stop();

        if (_displayNotificationHandle != IntPtr.Zero)
        {
            UnregisterPowerSettingNotification(_displayNotificationHandle);
            _displayNotificationHandle = IntPtr.Zero;
        }

        _messageWindow?.RemoveHook(WndProc);
        _messageWindow?.Dispose();
        _messageWindow = null;
    }
}
