using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using Caffeine_Pro.Converters;

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
        return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline;
    }

    /// <summary>
    /// Returns the text of a text file embedded in the application resources
    /// </summary>
    /// <param name="resourceName">Path to the resource</param>
    public static string GetResourceTextFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName) ??
                           throw new Exception("Resource not found: " + resourceName);
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
        return assembly.GetManifestResourceStream(resourceName) ??
               throw new Exception("Resource not found: " + resourceName);
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

    public static DateTime GetDateTimeFromTimeSpan(TimeSpan time)
    {
        var day = DateTime.Now.Date;
        if (time < DateTime.Now.TimeOfDay)
        {
            day = day.AddDays(1);
        }

        return day.Add(time);
    }

    public static string GetTimeString(TimeSpan time, bool isRelative)
    {
        var h = time.Hours;
        var m = time.Minutes;
        if (isRelative)
        {
            return h == 0 ? $"{m:00}m" : $"{h:00}h : {m:00}m";
        }

        // Absolute
        var z = "AM";
        if (h > 12)
        {
            h -= 12;
            z = "PM";
        }

        return $"{h:00}:{m:00} {z}";
    }


    public static string GetDateTimeString(TimeSpan time)
    {
        return GetDateTimeString(GetDateTimeFromTimeSpan(time));
    }

    /// <summary>
    /// Returns text representation of a time. If the time is today, it will return the time only.
    /// Depending on the date of the time, it will return "Yesterday", "Tomorrow", "In 2 days", "In 3 days"
    /// plus the time.
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static string GetDateTimeString(DateTime datetime)
    {
        if (datetime == DateTime.MaxValue)
        {
            return "Indefinitely";
        }

        if (datetime == DateTime.MinValue)
        {
            return "Inactive";
        }

        var day = (datetime.Date - DateTime.Today).Days switch
        {
            -3 => "3 days ago",
            -2 => "2 days ago",
            -1 => "Yesterday",
            1 => "Tomorrow",
            2 => "In 2 days",
            3 => "In 3 days",
            _ => string.Empty
        };

        return (string.IsNullOrEmpty(day) ? string.Empty : day + " ") + datetime.ToString("hh:mm tt");
    }

    /// <summary>
    /// Adds or removes the program to/from the Windows startup
    /// </summary>
    /// <param name="active">When true, adds the program to Windows startups, and when false removes it</param>
    public static void AddToWindowsStartup(bool active)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        const string applicationName = "Caffeine Pro";

        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (active && !string.IsNullOrEmpty(exePath))
            key?.SetValue(applicationName, exePath);
        else
            key?.DeleteValue(applicationName, false);
    }

    /// <summary>
    /// Determines if the application is added to the Windows startup
    /// </summary>
    public static bool IsAddedToWindowsStartup()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
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

    public static string GetEnumDescription<T>(T value) where T : Enum
    {
        var fi = value?.GetType().GetField(value.ToString() ?? string.Empty);

        if (fi == null) return string.Empty;

        var attributes =
            (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

        return (attributes.Length > 0 ? attributes[0].Description : value?.ToString()) ?? string.Empty;
    }

    public static void OpenHyperlink(string uri)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });
    }

    public static void ShowMessageBox(string message, MessageBoxIcon icon = MessageBoxIcon.Information)
    {
        MessageBox.Show(message,
            Assembly.GetExecutingAssembly().GetName().Name,
            MessageBoxButtons.OK,
            icon);
    }


    /// <summary>
    /// Cleans up the session switch event
    /// </summary>
    ~Routines()
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
    }
}