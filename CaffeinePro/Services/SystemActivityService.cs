using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CaffeinePro.Services;

/// <summary>
/// Detects when the user returns to an idle machine, i.e. the computer becomes
/// active right after the display was turned off because of inactivity.
///
/// This complements <see cref="WindowsSessionService"/> (which only reports session
/// lock/unlock) by monitoring the monitor powering off/on
/// (GUID_CONSOLE_DISPLAY_STATE via WM_POWERBROADCAST), which does not raise a
/// session switch.
///
/// The same message-only window also listens for GUID_ACDC_POWER_SOURCE so that plugging
/// or unplugging the charger is reported the moment it happens.
/// </summary>
public sealed class SystemActivityService : IDisposable
{
    /// <summary>
    /// Raised when the computer becomes active again after the display was turned
    /// off due to inactivity.
    /// </summary>
    public event EventHandler? OnUserBecameActive;

    /// <summary>
    /// Raised when the machine switches between running on AC power and running on battery.
    /// </summary>
    public event EventHandler? OnPowerSourceChanged;

    private HwndSource? _messageWindow;
    private IntPtr _displayNotificationHandle = IntPtr.Zero;
    private IntPtr _powerSourceNotificationHandle = IntPtr.Zero;

    private DisplayState _lastDisplayState = DisplayState.On;

    public SystemActivityService()
    {
        _messageWindow = CreateMessageOnlyWindow();
        _messageWindow.AddHook(WndProc);

        _displayNotificationHandle =
            RegisterPowerSettingNotification(_messageWindow.Handle,
                ref GuidConsoleDisplayState, DeviceNotifyWindowHandle);

        _powerSourceNotificationHandle =
            RegisterPowerSettingNotification(_messageWindow.Handle,
                ref GuidAcdcPowerSource, DeviceNotifyWindowHandle);
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
            else if (setting.PowerSetting == GuidAcdcPowerSource)
            {
                // The payload distinguishes AC / battery / short-term (UPS), but callers only care
                // that the power source changed - they re-read the current status themselves.
                OnPowerSourceChanged?.Invoke(this, EventArgs.Empty);
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
    private static readonly IntPtr HwndMessage = new(-3);

    // ReSharper disable once InconsistentNaming
    private static Guid GuidConsoleDisplayState = new("6fe69556-704a-47a0-8f24-c28d936fda47");

    // ReSharper disable once InconsistentNaming
    private static Guid GuidAcdcPowerSource = new("5d3e9a59-e9d5-4b00-a6bd-ff34ff516548");

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

    // -- Cleanup --------------------------------------------------------------------------------

    public void Dispose()
    {
        if (_displayNotificationHandle != IntPtr.Zero)
        {
            UnregisterPowerSettingNotification(_displayNotificationHandle);
            _displayNotificationHandle = IntPtr.Zero;
        }

        if (_powerSourceNotificationHandle != IntPtr.Zero)
        {
            UnregisterPowerSettingNotification(_powerSourceNotificationHandle);
            _powerSourceNotificationHandle = IntPtr.Zero;
        }

        _messageWindow?.RemoveHook(WndProc);
        _messageWindow?.Dispose();
        _messageWindow = null;
    }
}
