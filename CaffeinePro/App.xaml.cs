using System.Drawing;
using System.Reflection;
using System.Windows;
using CaffeinePro.Classes;
using CaffeinePro.Services;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Notification.Wpf;
using Wpf.Ui.Appearance;
using MessageBox = System.Windows.MessageBox;

namespace CaffeinePro;

/// <summary>
/// Main application class
/// </summary>
public partial class App
{
    public static App CurrentApp => (App)Current;

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
                    .AddSingleton<SingletonService>()
                    .AddSingleton<ParameterProcessorService>()
                    .AddSingleton<NotificationManager>()
                    .AddSingleton<KeepAwakeService>()
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
        KeepAwakeService = _host.Services.GetRequiredService<KeepAwakeService>();
        ParameterProcessorService = _host.Services.GetRequiredService<ParameterProcessorService>();
        SingletonService = _host.Services.GetRequiredService<SingletonService>();
        AppSettings = _host.Services.GetRequiredService<AppSettings>();
        _host.Services.GetRequiredService<NotificationManager>();

    }

    /// <summary>
    /// Called when the application starts
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
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

        // Track Events
        KeepAwakeService.OnStatusChanged += OnStatusChanged;
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        AppDomain.CurrentDomain.UnhandledException += HandleException;
        TrayIcon = Routines.FindResource<TaskbarIcon>("TrayIcon")!;

        ApplyStartupAwakeness();

        // Update everything for the first time
        OnStatusChanged(this, EventArgs.Empty);
    }

    private void HandleException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");

            CurrentApp.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("An unexpected error occurred", "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }
    }


    private void ApplyStartupAwakeness()
    {
        // Process Startup Settings
        KeepAwakeService.Awakeness = AppSettings.StartupAwakeness;

        if (AppSettings.StartActive
            &&
            (
                KeepAwakeService.Awakeness.IsIndefinite
                ||
                KeepAwakeService.Awakeness.EndDateTime.TimeOfDay > DateTime.Now.TimeOfDay
            )
           )
        {
            KeepAwakeService.Activate();
        }

        Routines.AddToWindowsStartup(AppSettings.StartWithWindows);
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        //SetThemeColor();
    }

    private void SetThemeColor()
    {
        var isWindowsThemeDark = Routines.IsWindowsThemeDark();
        ApplicationThemeManager.Apply(isWindowsThemeDark ? ApplicationTheme.Dark : ApplicationTheme.Light);
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


        //TrayIcon!.Icon =
        //    KeepAwakeService.IsActive
        //        ? (KeepAwakeService.IsTemporarilyInactive ? _temporarilyInactiveIcon : _activeIcon)
        //        : _inactiveIcon
        //    ;
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
        AboutWindow.CloseIt(); // <- About Window might be open or loaded when exit is called
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
        AboutWindow.ShowIt();
    }

    /// <summary>
    /// Handling the "Exit" menu item
    /// </summary>
    private void OnExitMenu(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }


    private void OnSendFeedback(object sender, RoutedEventArgs e)
    {
        Routines.OpenHyperlink("https://lotrasoft.com/caffeinepro/feedback?product=Caffeine%20Pro");
    }


}