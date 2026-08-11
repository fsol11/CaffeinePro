using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel;
using Size = System.Windows.Size;

namespace CaffeinePro.Classes;

/// <summary>
/// This class contains various routines used throughout the application
/// </summary>
public static class Routines
{
    /// <summary>
    /// Opens a file or URL using the default application associated with it in Windows.
    /// </summary>
    /// <param name="hwnd">A handle to the parent window.</param>
    /// <param name="lpOperation">The operation to perform (e.g., "open", "edit").</param>
    /// <param name="lpFile">The file or URL to open.</param>
    /// <param name="lpParameters">Parameters for the operation.</param>
    /// <param name="lpDirectory">The default directory for the operation.</param>
    /// <param name="nShowCmd">Specifies how the application is to be shown when it is opened.</param>
    /// <returns>The return value is an instance-specific value that indicates the result of the operation.</returns>
    [DllImport("Shell32.dll")]
    private static extern int ShellExecuteA(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters,
        string lpDirectory, int nShowCmd);

    /// <summary>
    /// Finds the first ancestor of a given type in the visual tree of a WPF application.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="child"></param>
    /// <returns></returns>
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
    /// Finds a resource in the application resources and returns it as the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="resourceKey"></param>
    /// <returns></returns>
    public static T? FindResource<T>(string resourceKey)
    {
        return (T?)App.CurrentApp.FindResource(resourceKey);
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
    /// Mirrors the native SYSTEM_POWER_STATUS structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    /// <summary>
    /// Determines if the system is running on battery.
    /// </summary>
    /// <returns>
    /// True while the machine runs on battery. A machine without a battery reports the AC line
    /// status as online or unknown, so desktops - and any failure to read the power status - are
    /// reported as not on battery rather than pausing the service on a machine that cannot run out
    /// of power.
    /// </returns>
    public static bool IsOnBattery()
    {
        // AcLineStatus: 0 = offline (running on battery), 1 = online, 255 = unknown.
        const byte offline = 0;
        return GetSystemPowerStatus(out var status) && status.AcLineStatus == offline;
    }

    public static DateTime GetDateTimeFromTimeSpan(
        TimeSpan timespan,
        Awakeness.AwakenessTypes type = Awakeness.AwakenessTypes.Absolute)
    {
        var baseDate = type == Awakeness.AwakenessTypes.Absolute ? DateTime.Now.Date : Awakeness.GetNow();
        var datetime = baseDate.Add(timespan);
        while (datetime < DateTime.Now)
        {
            datetime = datetime.AddDays(1);
        }

        return datetime;
    }

    /// <summary>
    /// Returns the time between NOW and the passed DATETIME.
    /// If the DATETIME is passed, it will return relative time until TOMORROW the same time.
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns>
    /// The remaining time, or <see cref="TimeSpan.MaxValue"/> when the datetime is
    /// <see cref="DateTime.MaxValue"/> (an indefinite awakeness).
    /// </returns>
    public static TimeSpan ToRelativeTime(DateTime datetime)
    {
        if (datetime == DateTime.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        // Awakeness.GetNow() is used (rather than DateTime.Now) because the whole awakeness model
        // works at whole-minute resolution. Subtracting the seconds-accurate DateTime.Now here would
        // make the round-trip (time -> relative span -> time) land up to a minute off the time the
        // user actually picked.
        var span = datetime - Awakeness.GetNow();

        // A time that has already passed refers to the same time tomorrow.
        while (span <= TimeSpan.Zero)
        {
            span += TimeSpan.FromDays(1);
        }

        return span;
    }


    /// <summary>
    /// Returns text representation of a time. If the time is today, it will return the time only.
    /// Depending on the date of the time, it will return "Yesterday", "Tomorrow", "In 2 days", "In 3 days"
    /// plus the time.
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static string GetDateTimeString(DateTime datetime, bool includeDate = true)
    {
        var day = includeDate ? GetDateString(datetime) : string.Empty;
        return (datetime == DateTime.MaxValue) ? day : (day + datetime.ToString(" h:mm tt")).Trim();
    }

    /// <summary>
    /// Converts datetime to user friendly display string 
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static string GetDateString(DateTime datetime)
    {
        if (datetime == DateTime.MaxValue)
            return "♾️ Indefinitely";

        if (datetime == DateTime.MinValue)
            return "Inactive";

        if (datetime is { Hour: 0, Minute: 0 })
            return "Midnight";

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

    /// <summary>
    /// Returns full path of the executable program
    /// </summary>
    /// <returns></returns>
    public static string GetExePath()
    {
        return Process.GetCurrentProcess().MainModule?.FileName!;
    }

    /// <summary>
    /// Returns name of the application
    /// </summary>
    /// <returns></returns>
    private static string GetApplicationName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
    }

    /// <summary>
    /// Opens a registry key
    /// </summary>
    /// <returns></returns>
    private static RegistryKey? OpenAppRegistryKey()
    {
        return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
    }

    /// <summary>
    /// Identifier of the startup task declared in the MSIX package manifest. Must stay in sync
    /// with the TaskId in "CaffeinePro Setup\Package.appxmanifest".
    /// </summary>
    private const string StartupTaskId = "CaffeineProStartupTask";

    /// <summary>
    /// Retrieves the package full name of the calling process, or ERROR_NO_PACKAGE_IDENTITY (15700)
    /// when the process is not running from an MSIX package.
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength,
        StringBuilder? packageFullName);

    private static bool? _isPackaged;

    /// <summary>
    /// True when the application runs from an MSIX package (a Microsoft Store install) rather than
    /// from the MSI installer. The two builds start with Windows in completely different ways.
    /// </summary>
    public static bool IsPackaged
    {
        get
        {
            const int errorNoPackageIdentity = 15700;
            var length = 0;
            return _isPackaged ??= GetCurrentPackageFullName(ref length, null) != errorNoPackageIdentity;
        }
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

        if (IsPackaged)
        {
            SetPackagedStartupTask(isActive);
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
        if (IsPackaged)
        {
            return GetPackagedStartupTaskState() is StartupTaskState.Enabled
                or StartupTaskState.EnabledByPolicy;
        }

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
    /// Reads the current state of the startup task declared in the package manifest. The WinRT call
    /// is pushed onto a worker thread so that blocking on it cannot deadlock the UI dispatcher.
    /// </summary>
    private static StartupTaskState GetPackagedStartupTaskState()
    {
        try
        {
            return Task.Run(async () =>
                (await StartupTask.GetAsync(StartupTaskId).AsTask()).State).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // The extension is missing or the platform refused the query; report "not enabled"
            // rather than tearing down the app over a checkbox.
            return StartupTaskState.Disabled;
        }
    }

    /// <summary>
    /// Enables or disables the packaged startup task. Once the user turns the task off from Task
    /// Manager or Settings, Windows will not let the app turn it back on, so that case is reported
    /// instead of failing silently.
    /// </summary>
    /// <param name="isActive">When true, enables the startup task, and when false disables it</param>
    private static void SetPackagedStartupTask(bool isActive)
    {
        StartupTaskState state;

        try
        {
            state = Task.Run(async () =>
            {
                var task = await StartupTask.GetAsync(StartupTaskId).AsTask();

                if (!isActive)
                {
                    task.Disable();
                    return task.State;
                }

                return await task.RequestEnableAsync().AsTask();
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error Changing Startup Setting",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!isActive || state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
        {
            return;
        }

        var reason = state == StartupTaskState.DisabledByPolicy
            ? "Your organization's policy prevents Caffeine Pro from starting with Windows."
            : "Caffeine Pro was turned off in the Startup apps list, so it can only be re-enabled "
              + "from there. Open Task Manager > Startup apps (or Settings > Apps > Startup) and "
              + "switch Caffeine Pro on.";

        MessageBox.Show(reason, "Start With Windows", MessageBoxButton.OK, MessageBoxImage.Information);
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

    /// <summary>
    /// Indicates whether the workstation is currently locked. This value is updated based on session switch events.
    /// </summary>
    private static bool? _isWorkstationLocked;

    /// <summary>
    /// Opens the input desktop to check if the workstation is locked. If the input desktop cannot be opened, it is assumed that the workstation is locked.
    /// </summary>
    /// <param name="dwFlags">Specifies the desktop access flags.</param>
    /// <param name="fInherit">Indicates whether the handle can be inherited by child processes.</param>
    /// <param name="dwDesiredAccess">Specifies the access rights for the desktop.</param>
    /// <returns>A handle to the input desktop, or IntPtr.Zero if the desktop cannot be opened.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(int dwFlags, bool fInherit, int dwDesiredAccess);

    /// <summary>
    /// Closes the handle to a desktop that was opened using OpenInputDesktop. This is used to clean up resources after checking if the workstation is locked.
    /// </summary>
    /// <param name="hDesktop">A handle to the desktop to be closed.</param>
    /// <returns>True if the desktop handle was successfully closed; otherwise, false.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr hDesktop);

    /// <summary>
    /// Checks if the workstation is initially locked by attempting to open the input desktop. If the input desktop cannot be opened, it is assumed that the workstation is locked.
    /// </summary>
    /// <returns>True if the workstation is initially locked; otherwise, false.</returns>
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

    /// <summary>
    /// Handles the SessionSwitch event to update the _isWorkstationLocked variable based on the session switch reason. This method is called whenever a session switch event occurs, such as when the workstation is locked or unlocked.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A SessionSwitchEventArgs that contains the event data.</param>
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

    /// <summary>
    /// Returns true if Windows is in Dark Mode
    /// </summary>
    /// <returns>True if Windows is in Dark Mode; otherwise, false.</returns>
    public static bool IsWindowsThemeDark()
    {
        const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string registryValueName = "AppsUseLightTheme";

        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath);
        var registryValue = key?.GetValue(registryValueName);
        return registryValue is <= 0;
    }

    /// <summary>
    /// Returns the description attribute of an enum item.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value.</param>
    /// <returns>The description of the enum value, or the enum value as a string if no description is found.</returns>
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

    /// <summary>
    /// Opens a browser window and goes to the given URL.
    /// </summary>
    /// <param name="uri">The URL to open in the browser.</param>
    public static void OpenHyperlink(string uri) => _ = ShellExecuteA(IntPtr.Zero, "open", uri, "", "", 1);

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

    /// <summary>
    /// Converts a XAML resource to an Icon. The XAML resource should be a FrameworkElement (like a UserControl or Canvas) that represents the icon.
    /// </summary>
    /// <param name="resourceKey">The key of the XAML resource to convert.</param>
    /// <returns>An Icon created from the XAML resource.</returns>
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

    /// <summary>
    /// Converts a XAML resource to an ImageSource. The XAML resource should be a FrameworkElement (like a UserControl or Canvas) that represents the image.
    /// </summary>
    /// <param name="resourceKey">The key of the XAML resource to convert.</param>
    /// <returns>An ImageSource created from the XAML resource.</returns>
    public static ImageSource ConvertXamlToImageSource(string resourceKey)
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

        return renderTarget;
    }

    /// <summary>
    /// Converts content of an element (string, Button, or TextBlock) to a TimeSpan. 
    /// The content can be in various formats, such as "05:30 PM", "17:30", or a Button/TextBlock with the time as its content.
    /// </summary>
    /// <param name="content">The content to convert to a TimeSpan.</param>
    /// <returns>A TimeSpan representing the time, or TimeSpan.MaxValue if the content cannot be converted.</returns>
    public static TimeSpan ContentToTimeSpan(object? content)
    {
        switch (content)
        {
            case string text:
                {
                    var s = text.Trim();
                    if (s.Length == 0)
                    {
                        return TimeSpan.MaxValue;
                    }

                    // The callers below append the AM/PM marker of the button to content that may
                    // already end with one (e.g. "05:30PMPM"), so strip every trailing marker.
                    var isAm = false;
                    var isPm = false;
                    while (EndsWithMarker(s, "AM") || EndsWithMarker(s, "PM"))
                    {
                        isAm |= EndsWithMarker(s, "AM");
                        isPm |= EndsWithMarker(s, "PM");
                        s = s[..^2].TrimEnd();
                    }

                    int hour;
                    var minute = 0;
                    var colon = s.IndexOf(':');

                    if (colon == -1)
                    {
                        if (!int.TryParse(s, out hour))
                        {
                            return TimeSpan.MaxValue;
                        }
                    }
                    else if (!int.TryParse(s[..colon], out hour) || !int.TryParse(s[(colon + 1)..], out minute))
                    {
                        return TimeSpan.MaxValue;
                    }

                    switch (hour)
                    {
                        case < 12 when isPm:
                            hour += 12;
                            break;
                        case 12 when isAm: // <- 12 AM is midnight, not noon
                            hour = 0;
                            break;
                    }

                    return hour is < 0 or > 23 || minute is < 0 or > 59
                        ? TimeSpan.MaxValue
                        : new TimeSpan(hour, minute, 0);
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

    private static bool EndsWithMarker(string text, string marker) =>
        text.EndsWith(marker, StringComparison.CurrentCultureIgnoreCase);
}