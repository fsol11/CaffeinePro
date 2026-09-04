using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Threading;
using CaffeinePro.Classes;
using CaffeinePro.Localization;
using CaffeinePro.Services;
using CaffeinePro.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Notification.Wpf;
using Wpf.Ui.Appearance;
using MenuItem = System.Windows.Controls.MenuItem;
using TrayPoint = Hardcodet.Wpf.TaskbarNotification.Interop.Point;

namespace CaffeinePro;

/// <summary>
/// Main application class
/// </summary>
public partial class App
{
    public static App CurrentApp => (Current as App)!;

    public static string AppName => Assembly.GetExecutingAssembly().GetName().Name!;
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    //private ImageSource _activeIcon;
    //private ImageSource _inactiveIcon;
    //private ImageSource _temporarilyInactiveIcon;
    private Icon _activeIcon = null!;
    private Icon _inactiveIcon = null!;
    private Icon _temporarilyInactiveIcon = null!;

    private readonly ILogger<App> _logger;

    public KeepAwakeService KeepAwakeService
    {
        get;
    }

    private SingletonService SingletonService
    {
        get;
    }

    private ParameterProcessorService ParameterProcessorService
    {
        get;
    }

    /// <summary>
    /// Owns the application's system-wide keyboard shortcuts (currently the blackout screen).
    /// </summary>
    public HotKeyService HotKeyService
    {
        get;
    }

    public AppSettings AppSettings
    {
        get;
    }

    private readonly IHost _host =
        Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Register your services here
                services
                    .AddSingleton<WindowsSessionService>()
                    .AddSingleton<SystemActivityService>()
                    .AddSingleton<SingletonService>()
                    .AddSingleton<ParameterProcessorService>()
                    .AddSingleton<NotificationManager>()
                    .AddSingleton<KeepAwakeService>()
                    .AddSingleton<HotKeyService>()
                    .AddSingleton(AppSettings.Load())
                    .AddLogging(logging =>
                    {
                        logging
                            .SetMinimumLevel(LogLevel.Information)
                            .AddEventLog()
                            .AddDebug();
                    });
            })
            
            .Build();

    public App()
    {
        _logger = _host.Services.GetRequiredService<ILogger<App>>();

        // The settings come first, and the language is applied straight away - ahead of every other
        // service. Some of them build display text in their constructor (KeepAwakeService composes
        // the tray tooltip), and that has to happen under the right culture rather than be
        // corrected afterwards.
        AppSettings = _host.Services.GetRequiredService<AppSettings>();
        LocalizationService.Instance.Apply(AppSettings.Language);

        KeepAwakeService = _host.Services.GetRequiredService<KeepAwakeService>();
        ParameterProcessorService = _host.Services.GetRequiredService<ParameterProcessorService>();
        SingletonService = _host.Services.GetRequiredService<SingletonService>();
        HotKeyService = _host.Services.GetRequiredService<HotKeyService>();
        _host.Services.GetRequiredService<NotificationManager>();
    }

    /// <summary>
    /// Called when the application starts
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        //_activeIcon = Routines.ConvertXamlToImageSource("ActiveIconCanvas");
        //_inactiveIcon = Routines.ConvertXamlToImageSource("InactiveIconCanvas");
        //_temporarilyInactiveIcon = Routines.ConvertXamlToImageSource("TemporarilyInactiveIconCanvas");

        _activeIcon = Routines.ConvertXamlToIcon("ActiveIconCanvas");
        _inactiveIcon = Routines.ConvertXamlToIcon("InactiveIconCanvas");
        _temporarilyInactiveIcon = Routines.ConvertXamlToIcon("TemporarilyInactiveIconCanvas");

        ParameterProcessorService.ShowHelpAndExitIfRequested(e.Args);
        SingletonService.IfAnotherInstanceExistsSendArgsAndExit(e.Args);
        ParameterProcessorService.ProcessArgs(e.Args);
        Init();
        base.OnStartup(e);
    }

    /// <summary>
    /// Initialize the application
    /// </summary>
    private void Init()
    {
        SetThemeColor();

        // Every menu item, wherever it is built, gets its submenu chevron turned round in a
        // right-to-left language. Class handlers rather than a style, so nothing has to remember to
        // opt in - and so this also covers items generated from a menu's ItemsSource.
        //
        // Two hooks are needed. Loaded catches the items of a menu as it is shown, but it never
        // arrives for some items nested inside a submenu, which is why the Language entry kept its
        // arrow pointing the wrong way; SubmenuOpened covers those, firing on the parent at the
        // moment its children are realized.
        EventManager.RegisterClassHandler(typeof(MenuItem), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnMenuItemLoaded));
        EventManager.RegisterClassHandler(typeof(MenuItem), MenuItem.SubmenuOpenedEvent,
            new RoutedEventHandler(OnSubmenuOpened));

        // Track Events
        KeepAwakeService.OnStatusChanged += OnStatusChanged;
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        AppDomain.CurrentDomain.UnhandledException += HandleException;
        
        TrayIcon = Routines.FindResource<TaskbarIcon>("TrayIcon")!;

        // The tray icon's own right-click handling does not survive - see OnTrayRightMouseUp.
        TrayIcon.TrayRightMouseUp += OnTrayRightMouseUp;

        Routines.AddToWindowsStartup(AppSettings.StartWithWindows);

        // The settings file is read - and its awakeness texts built - while the host is still being
        // constructed, before the language is known. Running the switch-over once now brings that
        // cached text into line, exactly as picking a language from the menu would.
        OnLanguageChanged(this, EventArgs.Empty);

        HotKeyService.BlackoutRequested += (_, _) => BlackoutWindow.ToggleIt();
        HotKeyService.Start();

        KeepAwakeService.ConfirmAndSetDefaultAwakeness();

        // Update icon
        OnStatusChanged(this, EventArgs.Empty);
    }

    private void HandleException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");

            CurrentApp.Dispatcher.Invoke(() =>
            {
                Dialogs.ShowError(LocalizationService.Get("Error_Unexpected"));
            });
        }
    }

    /// <summary>
    /// Points a menu item's submenu chevron the way the language reads.
    /// </summary>
    /// <remarks>
    /// The chevron is a glyph from an icon font, and <see cref="FlowDirection"/> does not mirror
    /// glyphs the way it mirrors layout and vector art - so in Arabic and Farsi the submenu opened
    /// to the left while its arrow still pointed right. The theme names the glyph "Chevron"; every
    /// other icon, and the drop-down button's own chevron - which points down, and reads the same
    /// either way - is left alone.
    /// </remarks>
    private static void OnMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            MirrorSubmenuChevron(item, allowRetry: true);
        }
    }

    /// <summary>
    /// Turns round the chevrons of the items a submenu has just revealed.
    /// </summary>
    /// <remarks>
    /// Queued rather than run immediately: at the moment the event is raised the containers are
    /// being generated, and their templates - and so the chevron - are only in place once the
    /// dispatcher has finished loading them.
    /// </remarks>
    private static void OnSubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem parent)
        {
            return;
        }

        parent.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            foreach (var entry in parent.Items)
            {
                // Items written straight into the menu are their own containers; items coming from
                // an ItemsSource have one generated for them.
                var child = parent.ItemContainerGenerator.ContainerFromItem(entry) as MenuItem
                            ?? entry as MenuItem;

                if (child != null)
                {
                    MirrorSubmenuChevron(child, allowRetry: false);
                }
            }
        });
    }

    /// <summary>
    /// Binds one menu item's chevron to <see cref="LocalizationService.MirrorTransform"/>.
    /// </summary>
    /// <param name="allowRetry">
    /// True on the first attempt. An item nested inside a submenu can raise Loaded before its
    /// template has been applied, and the chevron simply is not there yet to be found; the retry
    /// comes back for it once the dispatcher has finished loading that level of the menu. Without
    /// it the top-level items were turned round and the ones a level down were not.
    /// </param>
    private static void MirrorSubmenuChevron(MenuItem item, bool allowRetry)
    {
        item.ApplyTemplate();

        if (item.Template?.FindName("Chevron", item) is not FrameworkElement chevron)
        {
            if (allowRetry)
            {
                item.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    () => MirrorSubmenuChevron(item, allowRetry: false));
            }

            return;
        }

        if (BindingOperations.IsDataBound(chevron, UIElement.RenderTransformProperty))
        {
            return;
        }

        chevron.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        BindingOperations.SetBinding(chevron, UIElement.RenderTransformProperty,
            new Binding(nameof(LocalizationService.MirrorTransform))
            {
                Source = LocalizationService.Instance,
                Mode = BindingMode.OneWay,
            });
    }

    /// <summary>
    /// Handling the language menu: stores the choice and switches the UI over to it.
    /// </summary>
    /// <remarks>
    /// No restart is needed - every piece of text on screen is bound to
    /// <see cref="LocalizationService"/>, and the handful of strings that are cached instead are
    /// rebuilt by <see cref="OnLanguageChanged"/>.
    /// </remarks>
    private void OnLanguageMenu(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: AppLanguage language })
        {
            return;
        }

        AppSettings.Language = language.Code;
        LocalizationService.Instance.Apply(language.Code);
    }

    /// <summary>
    /// Brings the text that is built in C# - rather than bound in XAML - into the new language.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        KeepAwakeService.RefreshLocalizedTexts();
        AppSettings.RefreshLocalizedTexts();

        // Last, so it picks up the values the two calls above have just rebuilt.
        UiRefresher.RefreshAll();
    }


    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // Windows raises this (Category General) when the user switches between light and dark mode.
        // The event may arrive on a non-UI thread, so marshal the theme update back onto the dispatcher.
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.Invoke(SetThemeColor);
        }
    }

    private static void SetThemeColor()
    {
        var target = Routines.IsWindowsThemeDark() ? ApplicationTheme.Dark : ApplicationTheme.Light;

        // Only re-apply when the theme actually changed to avoid redundant resource swaps,
        // since UserPreferenceChanged (General) fires for many unrelated preferences.
        if (ApplicationThemeManager.GetAppTheme() != target)
        {
            ApplicationThemeManager.Apply(target);
        }
    }

    /// <summary>
    /// Track the status changes to update the tray icon
    /// </summary>
    private void OnStatusChanged(object? sender = null, EventArgs? e = null)
    {
        Dispatcher.Invoke(UpdateTrayIcon);
    }

    private void UpdateTrayIcon()
    {
        TrayIcon!.Icon =
            (KeepAwakeService.IsActive
                ? (KeepAwakeService.IsTemporarilyInactive ? _temporarilyInactiveIcon : _activeIcon)
                : _inactiveIcon)
            ;
    }

    internal TaskbarIcon? TrayIcon
    {
        get;
        private set;
    }

    /// <summary>
    /// Called when the application exits to clean up the resources
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        AboutWindow.CloseIt(); // <- About Window might be open or loaded when exit is called
        BlackoutWindow.CloseIt();
        HotKeyService.Dispose(); // <- releases the system-wide shortcut registration
        //_activeIcon!.Dispose();
        //InactiveIcon!.Dispose();
        //_temporarilyInactiveIcon!.Dispose();
        base.OnExit(e);
    }


    /// <summary>
    /// Handling the "About" menu item
    /// </summary>
    private void OnAboutMenu(object sender, RoutedEventArgs e)
    {
        try
        {
            AboutWindow.ShowIt();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open About window");
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unhandled dispatcher exception");
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogError(ex, "Unhandled AppDomain exception (terminating: {IsTerminating})", e.IsTerminating);
        }
    }

    private void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    /// <summary>
    /// Handling the "Quit" menu item
    /// </summary>
    private void OnTrayContextMenuOpened(object sender, RoutedEventArgs e)
    {
        NotificationWindow.CloseIt();
    }

    /// <summary>
    /// Opens the tray menu on a right-click, and keeps it open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A right-click reaches Hardcodet.NotifyIcon.Wpf as WM_CONTEXTMENU rather than through
    /// <see cref="TaskbarIcon.MenuActivation"/>, which the library only ever reads for the left
    /// button. It does open the menu on that message - but it then activates the popup's window
    /// in the same breath, and the popup has no window yet at that point. The activation lands on
    /// the library's own hidden message window instead, the menu is left without the focus, and
    /// the shell takes the foreground back half a second later and closes it: the menu flashes up
    /// and vanishes, which reads as a right-click that does nothing at all. Left-clicking escapes
    /// this because the library holds that one back by the double-click interval, by which time
    /// the popup does have a window.
    /// </para>
    /// <para>
    /// So the menu is opened from the right-button-up - whichever of the two messages arrives
    /// first does the opening - and the activation is deferred until the window it needs exists.
    /// </para>
    /// </remarks>
    private void OnTrayRightMouseUp(object sender, RoutedEventArgs e)
    {
        if (TrayIcon?.ContextMenu is not { } menu)
        {
            return;
        }

        // Left where it is when it is already up: WM_CONTEXTMENU may have got in first, and the
        // menu is then already placed.
        if (!menu.IsOpen)
        {
            var position = GetTrayCursorPosition();

            menu.Placement = PlacementMode.AbsolutePoint;
            menu.HorizontalOffset = position.X;
            menu.VerticalOffset = position.Y;
            menu.IsOpen = true;
        }

        // Queued rather than run here: the popup is given its window on a later dispatcher pass,
        // and activating it is the whole point - an unfocused menu is the one that closes itself.
        menu.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (menu.IsOpen && PresentationSource.FromVisual(menu) is HwndSource source)
            {
                SetForegroundWindow(source.Handle);
                menu.Focus();
            }
        });
    }

    /// <summary>
    /// The cursor position to open the tray menu at, in the coordinates a popup is placed in.
    /// </summary>
    /// <remarks>
    /// Mirrors what the library does for the left button. Which of the two cursor calls applies
    /// depends on the notification protocol the icon ended up registered with, and
    /// <see cref="TaskbarIcon.SupportsCustomToolTips"/> is the one place that reports it.
    /// </remarks>
    private TrayPoint GetTrayCursorPosition()
    {
        var cursor = new TrayPoint();

        if (TrayIcon!.SupportsCustomToolTips)
        {
            GetPhysicalCursorPos(ref cursor);
        }
        else
        {
            GetCursorPos(ref cursor);
        }

        return TrayInfo.GetDeviceCoordinates(cursor);
    }

    /// <summary>
    /// Retrieves the cursor position in physical screen coordinates, which is what the shell
    /// reports positions in once an icon speaks the newer notification protocol.
    /// </summary>
    [DllImport("user32.dll")]
    private static extern bool GetPhysicalCursorPos(ref TrayPoint lpPoint);

    /// <summary>
    /// Retrieves the cursor position in the coordinates of the calling process.
    /// </summary>
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(ref TrayPoint lpPoint);

    /// <summary>
    /// Brings a window to the foreground, so that the menu inside it learns when it is deactivated.
    /// </summary>
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private void OnExitMenu(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }


    /// <summary>
    /// Handling the "Blackout Screen" menu item - the same action the blackout shortcut triggers.
    /// </summary>
    private void OnBlackoutMenu(object sender, RoutedEventArgs e)
    {
        BlackoutWindow.ToggleIt();
    }


    private void OnSendFeedback(object sender, RoutedEventArgs e)
    {
        Routines.OpenHyperlink("https://lotrasoft.com/feedback?product=Caffeine%20Pro");
    }


}