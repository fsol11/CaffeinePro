using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Size = System.Windows.Size;

namespace CaffeinePro.Classes;

/// <summary>
/// This class contains various routines used throughout the application
/// </summary>
public static class Routines
{
    public static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);

        // Traverse the visual tree
        while (parent != null && parent is not T)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent as T;
    }

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
        return System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus ==
               System.Windows.Forms.PowerLineStatus.Offline;
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
    private static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourceName) ??
               throw new Exception("Resource not found: " + resourceName);
    }


    public static DateTime GetDateTimeFromTimeSpan(TimeSpan time)
    {
        var datetime = DateTime.Now.Date.Add(time);
        if (datetime < DateTime.Now)
        {
            datetime = datetime.AddDays(1);
        }

        return datetime;
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
        var day = GetDateString(datetime);
        return (datetime == DateTime.MaxValue) ? day : day + datetime.ToString(" hh:mm tt");
    }

    public static string GetDateString(DateTime datetime)
    {
        if (datetime == DateTime.MaxValue)
        {
            return "Indefinitely";
        }

        if (datetime == DateTime.MinValue)
        {
            return "Inactive";
        }

        if (datetime is { Hour: 0, Minute: 0 })
        {
            return "Midnight";
        }

        var day = (datetime.Date - DateTime.Today).Days switch
        {
            -3 => "3 days ago",
            -2 => "2 days ago",
            -1 => "Yesterday",
            0 => string.Empty,
            1 => "Tomorrow",
            2 => "In 2 days",
            3 => "In 3 days",
            _ => datetime.ToString("MMM dd, yyyy")
        };

        return day;
    }

    public static string GetExePath()
    {
        return Process.GetCurrentProcess().MainModule?.FileName!;
    }

    private static string GetApplicationName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
    }

    private static RegistryKey? OpenAppRegistryKey()
    {
        return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
    }

    /// <summary>
    /// Adds or removes the program to/from the Windows startup
    /// </summary>
    /// <param name="isActive">When true, adds the program to Windows startups, and when false removes it</param>
    public static void AddToWindowsStartup(bool isActive)
    {
        if (isActive == IsAddedToWindowsStartup())
        {
            return;
        }

        using var key = OpenAppRegistryKey();
        if (isActive)
        {
            key?.SetValue(GetApplicationName(), GetExePath());
        }
        else
        {
            key?.DeleteValue(GetApplicationName(), false);
        }
    }

    /// <summary>
    /// Determines if the application is added to the Windows startup
    /// </summary>
    public static bool IsAddedToWindowsStartup()
    {
        var exePath = GetExePath();
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        using var key = OpenAppRegistryKey();
        return string.Equals(key?.GetValue(GetApplicationName())?.ToString(), exePath,
            StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// Determines if workstation is locked. However, as Windows does not provide a direct way to determine
    /// workstation is being locked, this method uses a workaround by monitoring session switch events.
    /// </summary>
    public static bool IsWorkstationLocked()
    {
        if (_isWorkstationLocked != null)
        {
            return _isWorkstationLocked.Value;
        }

        _isWorkstationLocked = IsWorkstationLockedInitial();

        // setting up the session monitoring
        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

        return _isWorkstationLocked.Value;
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
        var fi = value.GetType().GetField(value.ToString());

        if (fi == null)
        {
            return string.Empty;
        }

        var attributes = fi.GetCustomAttributes<DescriptionAttribute>(false).ToList();

        return attributes.Count != 0 ? attributes.First().Description : value.ToString();
    }

    public static void OpenHyperlink(string uri)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
    }

    private static T LoadXamlFromResource<T>(string resourceKey)
    {
        // Retrieve the XAML element using the provided key from the application resources
        var resourceDictionary = App.CurrentApp.Resources;
        if (resourceDictionary[resourceKey] is not T element)
        {
            throw new ArgumentException($"Resource with key '{resourceKey}' not found or is not a FrameworkElement.");
        }

        return element;
    }

    public static Icon ConvertXamlToIcon(string resourceKey)
    {
        var element = LoadXamlFromResource<FrameworkElement>(resourceKey);

        // Set the desired size
        element.Width = 16;
        element.Height = 16;

        // Measure and arrange the element
        element.Measure(new Size(element.Width, element.Height));
        element.Arrange(new Rect(0, 0, element.Width, element.Height));

        // Render the element to a RenderTargetBitmap
        var renderTarget = new RenderTargetBitmap(
            (int)element.Width, (int)element.Height,
            96, 96,
            PixelFormats.Pbgra32);
        renderTarget.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));
        using var memoryStream = new MemoryStream();
        encoder.Save(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var bitmap = new Bitmap(memoryStream);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public static TimeSpan ContentToTimeSpan(object? content)
    {
        switch (content)
        {
            case string text:
            {
                int hour;
                int minute;
                text = text.Trim();
                switch (text.Length)
                {
                    case 0:
                        return TimeSpan.MaxValue;
                    case <= 4:
                        hour = int.Parse(text[0..2]);
                        minute = 0;
                        break;
                    default:
                    {
                        var i = text.IndexOf(':');
                        if (i == -1)
                        {
                            return TimeSpan.MaxValue;
                        }

                        hour = int.Parse(text[..i]);
                        minute = int.Parse(text[(i + 1)..(i + 3)]);
                        break;
                    }
                }


                if (text.EndsWith("PM", StringComparison.CurrentCultureIgnoreCase) && hour < 12)
                {
                    hour += 12;
                }

                return new TimeSpan(hour, minute, 0);
            }

            case Button btn:
            {
                var text =
                    (btn.Content is TextBlock textBlock)
                        ? string.Concat(textBlock.Inlines.OfType<Run>().Select(r => r.Text.Trim()))
                        : btn.Content;


                if (btn.Tag is "AM" or "PM")
                {
                    text += (string)btn.Tag;
                }

                return ContentToTimeSpan(text);
            }

            case TextBlock textBlock: // <- Hours and minutes and AMPM (e.g. 05:30 PM)
            {
                var text =
                    string.Concat(textBlock.Inlines.OfType<Run>().Select(r => r.Text.Trim())) +
                    Convert.ToString(textBlock.Tag);
                return ContentToTimeSpan(text);
            }
        }

        return TimeSpan.MaxValue;
    }
}