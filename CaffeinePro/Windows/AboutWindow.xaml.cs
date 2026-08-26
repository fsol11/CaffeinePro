using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CaffeinePro.Classes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace CaffeinePro.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class AboutWindow
{
    // Entrance/exit animation parameters. The content choreography lives in the XAML storyboard;
    // only the window itself (which has no layout position to animate in XAML) is handled here.
    private const double EntranceRise = 26;
    private static readonly Duration EntranceDuration = new(TimeSpan.FromMilliseconds(280));
    private static readonly Duration ExitDuration = new(TimeSpan.FromMilliseconds(180));

    private double _restingTop;
    private bool _closingAnimated;

    private static AboutWindow? _window;

    public static string AppName => Assembly.GetExecutingAssembly().GetName().Name!;
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version!.ToString(2);

    /// <summary>
    /// A static function to create and show the About window. If the window is already open,
    /// it will be brought to the front.
    /// </summary>
    public static void ShowIt()
    {
        if (_window is { IsLoaded: true })
        {
            _window.Show();
            _window.Activate();
        }
        else
        {
            _window = new AboutWindow();
            _window.Show();
        }
    }

    /// <summary>
    /// Closes the About window if it is open
    /// </summary>
    public static void CloseIt()
    {
        if (_window is { IsLoaded: true })
        {
            // Forced close (e.g. application exit): skip the exit animation so the window
            // does not outlive the shutting-down dispatcher.
            _window._closingAnimated = true;
            _window.Close();
        }
    }

    /// <summary>
    /// Initializes the About window and sets up the commandline usage information text box
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        Opacity = 0;
    }

    /// <summary>
    /// Handle Hyperlink click. The URL that is set in Hyperlink will be opened in the default browser
    /// </summary>
    private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        Routines.OpenHyperlink(((Hyperlink)sender).NavigateUri.ToString());
    }

    private void AboutWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void AboutWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        AnimateIn();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_closingAnimated)
        {
            e.Cancel = true;
            AnimateOut();
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// Fades the window in while it rises into its final position.
    /// </summary>
    private void AnimateIn()
    {
        _restingTop = Top;
        Top = _restingTop + EntranceRise;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var rise = new DoubleAnimation(_restingTop, EntranceDuration) { EasingFunction = ease };
        rise.Completed += (_, _) =>
        {
            // Release the animations once finished, otherwise they keep holding the
            // property values and the window can no longer be dragged.
            BeginAnimation(TopProperty, null);
            Top = _restingTop;
        };

        var fade = new DoubleAnimation(1, EntranceDuration) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
        };

        BeginAnimation(TopProperty, rise);
        BeginAnimation(OpacityProperty, fade);
    }

    /// <summary>
    /// Fades the window out, then closes it for real.
    /// </summary>
    private void AnimateOut()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        var fade = new DoubleAnimation(0, ExitDuration) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            _closingAnimated = true;
            Close();
        };

        BeginAnimation(TopProperty, new DoubleAnimation(Top + EntranceRise / 2, ExitDuration) { EasingFunction = ease });
        BeginAnimation(OpacityProperty, fade);
    }
}
