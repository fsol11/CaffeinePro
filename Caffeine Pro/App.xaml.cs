using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Caffeine_Pro.Classes;
using Caffeine_Pro.WindowsAndControls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;

namespace Caffeine_Pro;

/// <summary>
/// Main application class
/// </summary>
public partial class App
{
    // Application services and settings
    public static readonly KeepAwakeService KeepAwakeService = new();
    public static readonly SingletonService SingletonService = new();
    public static readonly ParameterProcessorService ParameterProcessorService = new(KeepAwakeService);
    public static readonly AppSettings AppSettings = new();
    //public static readonly ApplicationTheme ApplicationTheme = Routines.IsWindowsThemeDark() ? ApplicationTheme.Dark : ApplicationTheme.Light;

    /// <summary>
    /// Called when the application starts
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // If the application is already running, send the arguments to the running instance
        if (!SingletonService.IsApplicationAlreadyRunning())
        {
            if (e.Args.Length == 0)
                MessageBox.Show("An instance of the application is already running.");
            else
                SingletonService.SendCommandToTheRunningInstance(string.Join(" ", e.Args));
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
        // track status changes to update the tray icon
        KeepAwakeService.OnStatusChanged += OnStatusChanged;

        // When another instance starts, it will send
        // its arguments to the running instance
        SingletonService.CommandReceived += (command) =>
        {
            ParameterProcessorService.ProcessArgs(command.Split(" "), ParameterProcessorService.StartActions.DoNothing);
        };

        // process the command line arguments
        ParameterProcessorService.ProcessArgs(e.Args, ParameterProcessorService.StartActions.Activate);

        SetThemeColor();

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
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
        SingletonService.Dispose(); // <- Disposing the singleton mutex
        base.OnExit(e);
    }

    /// <summary>
    /// Handling all the menu items for "Active For X" where X is the minutes
    /// All menu items have minutes as their tag
    /// </summary>
    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        var minutes = int.Parse((menu.Tag as string)!);
        KeepAwakeService.ActivateUntil(DateTime.Now.AddMinutes(minutes));
    }

    /// <summary>
    /// Handling all the menu items for "Activate Until X" where X is the time
    /// All menu items have time in their header
    /// </summary>
    private void MenuItemOnClick_ActivateUntil(object sender, RoutedEventArgs e)
    {
        var header = (string)((MenuItem)sender).Header;
        var hours = int.Parse(header[..2].Trim());
        var minutes = int.Parse(header[3..5]);
        var period = header[5..].Trim();
        if (period == "PM") hours += 12;
        var date = DateTime.Now.Date;
        var time = new TimeSpan(hours, minutes, 0);
        if (time < DateTime.Now.TimeOfDay) date = date.AddDays(1);
        KeepAwakeService.ActivateUntil(
            new DateTime(DateOnly.FromDateTime(date), TimeOnly.FromTimeSpan(time)));
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
    /// Handling +15 min button in the status control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPlus15(object? sender, EventArgs e)
    {
        KeepAwakeService.ActivateUntil(KeepAwakeService.UntilDateTime.AddMinutes(15));
    }

    /// <summary>
    /// Handling -15 min button in the status control
    /// </summary>
    private void OnMinus15(object? sender, EventArgs e)
    {
        KeepAwakeService.ActivateUntil(KeepAwakeService.UntilDateTime.AddMinutes(-15));
    }

    /// <summary>
    /// Handles the "Activate" menu item
    /// </summary>
    private void OnActivate(object sender, RoutedEventArgs e)
    {
        KeepAwakeService.ActivateUntil(DateTime.MaxValue);
    }

    /// <summary>
    /// Handles the "Cancel" menu item
    /// </summary>
    private void OnCancel(object sender, RoutedEventArgs e)
    {
        KeepAwakeService.Deactivate();
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

    /// <summary>
    /// Handles the "Reset Settings" menu item
    /// </summary>
    private void ResetSettings(object sender, RoutedEventArgs e)
    {
        AppSettings.Reset();
    }
}
