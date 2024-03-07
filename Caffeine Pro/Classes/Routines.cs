using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Management;
using System.Diagnostics;
using System.Drawing;

namespace Caffeine_Pro.Classes;

/// <summary>
/// This class contains various routines used throughout the application
/// </summary>
public class Routines
{
    /// <summary>
    /// Returns the current CPU usage of the system
    /// </summary>
    /// <returns></returns>
    public static float CpuUsage()
    {
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        return cpuCounter.NextValue();
    }

    private const int StdOutputHandle = -11;
    private static readonly IntPtr InvalidHandleValue = new(-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>
    /// Returns true if the application is running in a command line console
    /// </summary>
    public static bool IsConsole()
    {
        try
        {
            var handle = GetStdHandle(StdOutputHandle);
            return handle != IntPtr.Zero && handle != InvalidHandleValue;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines if the system is running on battery
    /// </summary>
    public static bool IsOnBattery()
    {
        var query = new ObjectQuery("SELECT * FROM Win32_Battery");
        using var searcher = new ManagementObjectSearcher(query);
        foreach (var mo in searcher.Get())
        {
            var status = mo["BatteryStatus"];
            if (status != null && int.TryParse(status.ToString(), out var statusValue))
            {
                // According to the Win32_Battery documentation, a value of 1 means "Discharging"
                if (statusValue == 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the text of a text file embedded in the application resources
    /// </summary>
    /// <param name="resourceName">Path to the resource</param>
    public static string GetResourceTextFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource not found: " + resourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Returns a stream of a resource embedded in the application resources
    /// </summary>
    /// <param name="resourceName">Path to the resource</param>
    public static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource not found: " + resourceName);
    }

    /// <summary>
    /// Returns an icon from a resource
    /// </summary>
    /// <param name="resourceName">Path to the icon resource</param>
    public static Icon GetIconFromResource(string resourceName)
    {
        using var iconStream = Routines.GetResourceStream("Caffeine_Pro.Resources.CoffeeDot.ico");
        return new Icon(iconStream);
    }

    /// <summary>
    /// Returns text representation of a time. If the time is today, it will return the time only.
    /// Depending on the date of the time, it will return "Yesterday", "Tomorrow", "In 2 days", "In 3 days"
    /// plus the time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static string GetTimeString(DateTime time)
    {
        var day = (time.Date - DateTime.Today).Days switch
        {
            -3 => "3 days ago",
            -2 => "2 days ago",
            -1 => "Yesterday",
            1 => "Tomorrow",
            2 => "In 2 days",
            3 => "In 3 days",
            _ => string.Empty
        };

        return (string.IsNullOrEmpty(day) ? string.Empty : day + " ") + time.ToShortTimeString();
    }

    /// <summary>
    /// Adds or removes the program to/from the Windows startup
    /// </summary>
    /// <param name="active">When true, adds the program to Windows startups, and when false removes it</param>
    public static void AddToWindowsStartup(bool active)
    {
        var exePath = Assembly.GetExecutingAssembly().Location;
        const string applicationName = "Caffeine Pro";

        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (active)
            key?.SetValue(applicationName, exePath);
        else
            key?.DeleteValue(applicationName, false);

    }

    /// <summary>
    /// Determines if the application is added to the Windows startup
    /// </summary>
    public static bool IsAddedToWindowsStartup()
    {
        var exePath = Assembly.GetExecutingAssembly().Location;
        var applicationName = Assembly.GetExecutingAssembly().GetName().Name!;

        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        return key?.GetValue(applicationName)?.ToString() == exePath;
    }

    /// <summary>
    /// Determines if workstation is locked. However, as Windows does not provide a direct way to determine
    /// workstation is being locked, this method uses a workaround by monitoring session switch events.
    /// </summary>
    public static bool IsWorkstationLocked()
    {
        if (_isWorkstationLocked != null) return _isWorkstationLocked == true;

        _isWorkstationLocked = IsWorkstationLockedInitial();

        // setting up the session monitoring
        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

        return _isWorkstationLocked == true;
    }

    private static bool? _isWorkstationLocked;
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(int dwFlags, bool fInherit, int dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr hDesktop);

    private static bool IsWorkstationLockedInitial()
    {
        const int switchDesktop = 0x0100;
        var hDesktop = OpenInputDesktop(0, false, switchDesktop);

        if (hDesktop == IntPtr.Zero)
        {
            // Could not get the input desktop, workstation is likely locked
            return true;
        }

        // Clean up
        CloseDesktop(hDesktop);
        return false;
    }

    private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        _isWorkstationLocked = e.Reason switch
        {
            SessionSwitchReason.SessionLock
                or SessionSwitchReason.RemoteDisconnect
                or SessionSwitchReason.ConsoleDisconnect
                or SessionSwitchReason.SessionLogoff
                => true,

            SessionSwitchReason.SessionUnlock
                or SessionSwitchReason.RemoteConnect
                or SessionSwitchReason.ConsoleConnect
                or SessionSwitchReason.SessionLogon
                => false,

            _ => _isWorkstationLocked
        };
    }

    public static bool IsWindowsThemeDark()
    {
        const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string registryValueName = "AppsUseLightTheme";

        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath);
        var registryValue = key?.GetValue(registryValueName);
        return registryValue is int and <= 0;
    }


    /// <summary>
    /// Cleans up the session switch event
    /// </summary>
    ~Routines()
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
    }
}