using System.Windows;
using CaffeinePro.Classes;
using CaffeinePro.Services;

namespace CaffeinePro.Windows;

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

    public static readonly DependencyProperty AwakenessProperty = DependencyProperty.Register(
        nameof(Awakeness),
        typeof(Awakeness),
        typeof(NotificationWindow),
        new PropertyMetadata(default(Awakeness)));

    /// <summary>
    /// The awakeness the notification is offering to activate. Starts out as the default (startup)
    /// awakeness and is replaced when the user picks a different time from the drop-down.
    /// </summary>
    public Awakeness Awakeness
    {
        get => (Awakeness)GetValue(AwakenessProperty);
        set => SetValue(AwakenessProperty, value);
    }

    /// <summary>
    /// The action taken once the awakeness ends, or <see cref="SessionAction.None"/> when there is none.
    /// </summary>
    public SessionAction AfterwardsAction => App.CurrentApp.AppSettings.AfterwardsAction;

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        ActivateAndClose(Awakeness);
    }

    /// <summary>
    /// A time picked from the drop-down. Its button reads "Apply &amp; Activate", so picking one is
    /// itself an answer to the prompt and there is nothing left to confirm.
    /// </summary>
    private void OnNewAwakenessSelected(object? sender, Awakeness awakeness)
    {
        ActivateAndClose(awakeness);
    }

    /// <summary>
    /// Answers the prompt with the given awakeness.
    /// </summary>
    /// <remarks>
    /// Renewed first, because the prompt can sit on screen for a long time: a relative awakeness
    /// ("for 2 hours") has to be counted from the moment it is accepted, not from the moment it was
    /// offered, and an absolute one ("until 5 PM") has to roll on to tomorrow once that time is past.
    /// </remarks>
    private static void ActivateAndClose(Awakeness awakeness)
    {
        App.CurrentApp.KeepAwakeService.Activate(Awakeness.RenewDateTime(awakeness));
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
