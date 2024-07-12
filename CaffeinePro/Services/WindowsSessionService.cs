using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CaffeinePro.Services;

public enum SessionAction
{
    [Description("Do Nothing")] None,
    [Description("Exit Program")] Exit,
    [Description("Lock")] Lock,
    [Description("Sign Out")] SignOut,
    [Description("Force Sign Out")] ForceSignOut,
    [Description("Shutdown")] Shutdown,
    [Description("Force Shutdown")] ForceShutdown,
    [Description("Restart")] Restart,
    [Description("Force Restart")] ForceRestart,
    [Description("Hibernate")] Hibernate,
    [Description("Sleep")] Sleep,
    [Description("Monitor Off")] MonitorOff
}

public sealed class WindowsSessionService
{
    public WindowsSessionService()
    {
        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
    }

    ~WindowsSessionService()
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
    }

    public event EventHandler? OnUnlock;
    public event EventHandler? OnLock;

    [DllImport("user32")]
    public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int EWX_LOGOFF = 0x0;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int EWX_POWEROFF = 0x00000008;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int EWX_REBOOT = 0x00000002;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int EWX_FORCE = 0x00000004;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int EWX_FORCEIFHUNG = 0x00000010;



    // P/Invoke declaration for SendMessage
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(int hWnd, int hMsg, int wParam, int lParam);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    [DllImport("PowrProf.dll", CharSet = CharSet.Auto,
        ExactSpelling = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int SC_MONITORPOWER = 0xF170;

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int WM_SYSCOMMAND = 0x0112;

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int MONITOR_OFF = 2;

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int HWND_BROADCAST = 0xFFFF;

    public void ExecuteSessionAction(SessionAction action)
    {
        bool result;
        switch (action)
        {
            case SessionAction.None: break;

            case SessionAction.Exit:
                App.CurrentApp.Shutdown(); break;

            case SessionAction.SignOut:
                result = ExitWindowsEx(EWX_LOGOFF, 0); break;

            case SessionAction.ForceSignOut:
                result = ExitWindowsEx(EWX_LOGOFF, EWX_FORCEIFHUNG); break;

            //case SessionAction.Shutdown:
            //    result = ExitWindowsEx(EWX_POWEROFF, 0); break;

            //case SessionAction.ForceShutdown:
            //    result=ExitWindowsEx(EWX_POWEROFF, EWX_FORCEIFHUNG); break;

            //case SessionAction.Restart:
            //    result = ExitWindowsEx(EWX_REBOOT, 0); break;

            //case SessionAction.ForceRestart:
            //    result = ExitWindowsEx(EWX_REBOOT, EWX_FORCEIFHUNG); break;


            case SessionAction.Shutdown:
                System.Diagnostics.Process.Start("shutdown", "/s");
                break;
            case SessionAction.ForceShutdown:
                System.Diagnostics.Process.Start("shutdown", "/s /f");
                break;
            case SessionAction.Restart:
                System.Diagnostics.Process.Start("shutdown", "/r");
                break;
            case SessionAction.ForceRestart:
                System.Diagnostics.Process.Start("shutdown", "/r /f");
                break;


            case SessionAction.Lock: LockWorkStation(); break;

            case SessionAction.Hibernate: 
                SetSuspendState(true, false, false); break;

            case SessionAction.Sleep: 
                SetSuspendState(false, false, false); break;

            case SessionAction.MonitorOff: 
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF); break;

            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLogoff:
            case SessionSwitchReason.SessionLock:
                OnLock?.Invoke(this, EventArgs.Empty);
                break;
            case SessionSwitchReason.SessionLogon:
            case SessionSwitchReason.SessionUnlock:
                OnUnlock?.Invoke(this, EventArgs.Empty);
                break;
        }
    }
}