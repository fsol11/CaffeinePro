using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Caffeine_Pro.Classes;
using Caffeine_Pro.WindowsAndControls;
using Hardcodet.Wpf.TaskbarNotification;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace Caffeine_Pro;

public partial class App : INotifyPropertyChanged
{
    private TaskbarIcon? _trayIcon;
    private ContextMenu? _menu;
    private StatusControl? _statusControl;
    private bool _isActive;

    private static readonly AboutWindow AboutWindow = new();
    private static readonly KeepAwakeService KeepAwakeService = new();
    private static readonly SingletonService SingletonService = new();
    private static readonly ParameterProcessorService ParameterProcessorService = new(KeepAwakeService);
    private static readonly Icon NormalIcon;
    private static readonly Icon ActiveIcon;

    static App()
    {
        using var iconStream1 = Routines.GetResourceStream("Caffeine_Pro.Resources.Coffee.ico");
        NormalIcon = new Icon(iconStream1);
        using var iconStream2 = Routines.GetResourceStream("Caffeine_Pro.Resources.CoffeeDot.ico");
        ActiveIcon = new Icon(iconStream2);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        if (!SingletonService.CheckSingleton())
        {
            if (e.Args.Length == 0)
                MessageBox.Show("An instance of the application is already running.");
            else
                SingletonService.SendCommandToTheRunningInstance(string.Join(" ", e.Args));
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Init();

        ParameterProcessorService.ProcessArgs(e.Args, ParameterProcessorService.StartActions.Activate);
        ApplyCommands();
    }

    private void ApplyCommands()
    {
        _trayIcon!.Visibility = AppSettings.Default.NoIcon ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Init()
    {
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon")!;
        _menu = (ContextMenu)FindResource("TrayContextMenu")!;
        _menu.DataContext = this;
        _statusControl = (StatusControl)((MenuItem)_menu.Items[0]!).Header!;
        KeepAwakeService.OnActivate += OnStatusChanged;
        KeepAwakeService.OnDeactivate += OnStatusChanged;
        KeepAwakeService.Tick += OnStatusChanged;

        // When another instance starts, it will send its arguments to the running instance
        SingletonService.CommandReceived += (command) =>
        {
            ParameterProcessorService.ProcessArgs(command.Split(" "), ParameterProcessorService.StartActions.DoNothing);
            ApplyCommands();
        };
    }

    private void OnStatusChanged(object? sender, EventArgs e)
    {
        RefreshStatus();
    }

    /// <summary>
    /// Sets tooltip and UI status based on the current date time
    /// </summary>
    private void RefreshStatus()
    {
        IsActive = KeepAwakeService.IsActive;
        _trayIcon!.Icon = IsActive ? ActiveIcon : NormalIcon;
        _trayIcon.ToolTipText = "Caffeine Pro";
        if (IsActive && KeepAwakeService.UntilDateTime != DateTime.MaxValue)
            _trayIcon.ToolTipText += $"\r\nActive Until: {Routines.GetTimeString(KeepAwakeService.UntilDateTime)}";

        if (IsActive)
            _statusControl!.SetActive(KeepAwakeService.UntilDateTime);
        else
            _statusControl!.SetInactive();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _trayIcon?.Dispose();
        NormalIcon.Dispose();
        ActiveIcon.Dispose();
        SingletonService.Dispose();
    }

    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        var minutes = int.Parse((menu.Tag as string)!);
        KeepAwakeService.ActivateUntil(DateTime.Now.AddMinutes(minutes));
    }

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
        KeepAwakeService.ActivateUntil(new DateTime(DateOnly.FromDateTime(date), TimeOnly.FromTimeSpan(time)));
    }


    private void OnAboutMenu(object sender, RoutedEventArgs e)
    {
        AboutWindow.ShowDialog();
    }

    private void OnExitMenu(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPlus15(object? sender, EventArgs e)
    {
        KeepAwakeService.ActivateUntil(KeepAwakeService.UntilDateTime.AddMinutes(15));
    }

    private void OnMinus15(object? sender, EventArgs e)
    {
        KeepAwakeService.ActivateUntil(KeepAwakeService.UntilDateTime.AddMinutes(-15));
    }

    private void OnActivate(object sender, RoutedEventArgs e)
    {
        KeepAwakeService.ActivateUntil(DateTime.MaxValue);
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        KeepAwakeService.Deactivate();
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if ((e.OriginalSource as TextBox)!.Text.Length + e.Text.Length > 2
            || !decimal.TryParse(e.Text, out var num)
            || num is > 99 or < 0)
        {
            // If not, cancel the input
            e.Handled = true;
        }

    }

    private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Check if the key is a space
        if (e.Key == Key.Space)
        {
            // If it is, cancel the key
            e.Handled = true;
        }
    }

    private void NoIconBtn(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                "The icon will be removed from the tray. If you continue, the only way to access the application will be through the command line. Continue?",
                "Alert",
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            _trayIcon!.Visibility = Visibility.Collapsed;
    }

    private void CpuUsage_OnGotFocus(object sender, RoutedEventArgs e)
    {
        (sender as TextBox)?.SelectAll();
    }

    private void ResetSettings(object sender, RoutedEventArgs e)
    {
        AppSettings.Default.Reset();
    }

    private void SetLanguageMenu(object sender, RoutedEventArgs e)
    {
        if (((MenuItem)sender).Tag is not string culture) return;
        AppSettings.Default.Culture = culture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
    }
}
