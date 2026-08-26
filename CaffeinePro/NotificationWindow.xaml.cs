using System.Windows;
using CaffeinePro.Classes;
using CaffeinePro.Services;

namespace CaffeinePro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class NotificationWindow
{
    // Singleton instance to ensure only one notification window is open at a time
    private static NotificationWindow? _window;

    /// <summary>
    /// Initializes the About window and sets up the commandline usage information text box
    /// </summary>
    public static void CloseIt()
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {

            if (_window is { IsLoaded: true })
            {
                _window.Close();
            }

            _window = null;
        });
    }

    /// <summary>
    /// Opens the notification window with the given awakeness information. If a notification is already open, it will be closed and replaced with the new one.
    /// </summary>
    /// <param name="aw"></param>
    public static void OpenIt(Awakeness aw)
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            if (_window is { IsLoaded: true })
            {
                CloseIt();
            }

            _window = new NotificationWindow(aw);
            _window.Show();
        });
    }

    /// <summary>
    /// Private constructor to enforce singleton pattern. Use OpenIt() to create and show the window.
    /// </summary>
    /// <param name="aw"></param>
    private NotificationWindow(Awakeness aw)
    {
        _window = this;
        Awakeness = aw;
        InitializeComponent();
    }

    /// <summary>
    /// The awakeness the notification is offering to activate.
    /// </summary>
    public Awakeness Awakeness
    {
        get;
    }

    /// <summary>
    /// The action taken once the awakeness ends, or <see cref="SessionAction.None"/> when there is none.
    /// </summary>
    public SessionAction AfterwardsAction => App.CurrentApp.AppSettings.AfterwardsAction;

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.KeepAwakeService.ActivateDefault();
        CloseIt();
    }

    private void IgnoreForToday_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.KeepAwakeService.SetIgnoreUnlockNotificationToToday();
        CloseIt();
    }

    private void AskLater_Click(object sender, RoutedEventArgs e)
    {
        CloseIt();
    }

    private void AlwaysActivate_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.KeepAwakeService.ActivateDefault();
        App.CurrentApp.AppSettings.StartActive = true;
        CloseIt();
    }
}
