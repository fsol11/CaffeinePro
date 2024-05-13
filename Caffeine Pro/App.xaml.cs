using System.Windows;
using System.Windows.Media.Imaging;
using Caffeine_Pro.Classes;
using Caffeine_Pro.Services;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;

namespace Caffeine_Pro;

/// <summary>
/// Main application class
/// </summary>
public partial class App
{
    public static App CurrentApp => (App)Current;

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

    private readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            // Register your services here
            services.AddSingleton<WindowsSessionService>();
            services.AddSingleton<KeepAwakeService>();
            services.AddSingleton<SingletonService>();
            services.AddSingleton<ParameterProcessorService>();
            services.AddSingleton(AppSettings.Load());
            // Add more services as needed
        })
        .Build();

    public App()
    {
        KeepAwakeService = _host.Services.GetRequiredService<KeepAwakeService>();
        ParameterProcessorService = _host.Services.GetRequiredService<ParameterProcessorService>();
        SingletonService = _host.Services.GetRequiredService<SingletonService>();
        AppSettings = _host.Services.GetRequiredService<AppSettings>();
    }

    /// <summary>
    /// Called when the application starts
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // If the application is already running, send the arguments to the running instance
        if (!SingletonService.IsThisTheOnlyInstance())
        {
            switch (e.Args.Length)
            {
                case 0:
                    Routines.ShowMessageBox("An instance of the application is already running.");
                    break;
                default:
                    SingletonService.SendCommandToTheRunningInstance(string.Join(" ", e.Args));
                    break;
            }

            Shutdown();
            return;
        }

        base.OnStartup(e);
        Init(e);
    }

    /// <summary>
    /// Initialize the application
    /// </summary>
    /// <param name="e"></param>
    private void Init(StartupEventArgs e)
    {
        var keepAwakeService = _host.Services.GetRequiredService<KeepAwakeService>();

        // track status changes to update the tray icon
        keepAwakeService.OnStatusChanged += OnStatusChanged;

        // process the command line arguments
        ParameterProcessorService.ProcessArgs(e.Args);

        SetThemeColor();

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        OnStatusChanged(this, EventArgs.Empty);
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        SetThemeColor();
    }

    private void SetThemeColor()
    {
        Resources.MergedDictionaries.OfType<ThemesDictionary>().First().Theme
            = Routines.IsWindowsThemeDark()
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
    }

    /// <summary>
    /// Track the status changes to update the tray icon
    /// </summary>
    private void OnStatusChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(UpdateTrayIcon);
    }

    private void UpdateTrayIcon()
    {
        // Updating icon
        ((TaskbarIcon)FindResource("TrayIcon")!).IconSource = (BitmapImage)(
            KeepAwakeService.IsActive
                ? FindResource("ActiveIcon")
                : FindResource("InactiveIcon")
        )!;
    }

    /// <summary>
    /// Called when the application exits to clean up the resources
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        AboutWindow.CloseIt(); // <- About Window might be open or loaded when exit is called
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

    /// <summary>
    /// Handling the "No Icon" menu item
    /// The menu as well as Tray Icon's visibility is bound to AppSettings, and is automatically saved.
    /// This function just confirms with the user that by removing the icon, the only way
    /// to communicate with the program will be through the command line. If the user does not
    /// want that, the icon is unhidden.
    /// </summary>
    private void NoIconBtn(object sender, RoutedEventArgs e)
    {
        if (AppSettings.NoIcon
            && MessageBox.Show(
                "If icon is removed, the only way to access the application will be through the command line. Continue?",
                "Alert", MessageBoxButton.OKCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.OK,
                MessageBoxOptions.DefaultDesktopOnly) != MessageBoxResult.OK
           )
        {
            AppSettings.NoIcon = false;
        }
    }
    

    private void OnSendFeedback(object sender, RoutedEventArgs e)
    {
        Routines.OpenHyperlink("https://lotrasoft.com/caffeinepro/feedback?product=Caffeine%20Pro");
    }
}