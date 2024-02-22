using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Management;
using System.Diagnostics;
using System.Windows.Controls;


namespace Caffeine_Pro.Classes;

public class Routines
{
    public static float CpuUsage()
    {
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        return cpuCounter.NextValue();
    }

    private const int StdOutputHandle = -11;
    private static readonly IntPtr InvalidHandleValue = new(-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

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

    public static string GetResourceTextFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource not found: " + resourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    public static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource not found: " + resourceName);
    }

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

    public static T? FindChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent == null) return null;

        T? foundChild = null;

        var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is not T childType)
            {
                foundChild = FindChild<T>(child);

                if (foundChild != null) break;
            }
            else
            {
                foundChild = childType;
                break;
            }
        }

        return foundChild;
    }

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


    private static bool? _isWorkstationLocked = null;
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

    public static bool IsWorkstationLocked()
    {
        if (_isWorkstationLocked == null)
        {
            _isWorkstationLocked = IsWorkstationLockedInitial();
            // setting up the session monitoring
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        return _isWorkstationLocked == true;
    }

    public static MenuItem? FindMenuItemByTag(ItemCollection menuItems, string tag)
    {
        foreach (MenuItem item in menuItems)
        {
            if ((string)item.Tag == tag) return item;

            if (!item.HasItems) continue;
            var foundItem = FindMenuItemByTag(item.Items, tag);
            if (foundItem != null) return foundItem;
        }

        return null;
    }

    ~Routines()
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
    }
}