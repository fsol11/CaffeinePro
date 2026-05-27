using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using CaffeinePro.Classes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace CaffeinePro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class AboutWindow
{
    private static AboutWindow? _window;

    private bool _isPlayingForward = true;
    // Change the declaration of _reverseTimer to nullable to fix CS8618
    private DispatcherTimer? _reverseTimer;

    public static string AppName => Assembly.GetExecutingAssembly().GetName().Name!;
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

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
            _window.Close();
        }
    }

    /// <summary>
    /// Initializes the About window and sets up the commandline usage information text box
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();

        Icon = new System.Windows.Media.Imaging.BitmapImage(
            new Uri("pack://application:,,,/Resources/Coffee.png"));

        PingPongMedia.Source = new Uri("pack://application:,,,/Resources/about.mp4");
        PingPongMedia.Play();
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

    private void PingPongMedia_MediaOpened(object sender, RoutedEventArgs e)
    {
        // Optionally handle when media is ready
    }

    private void PingPongMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (_isPlayingForward)
        {
            // Start reverse playback
            _isPlayingForward = false;
            StartReversePlayback();
        }
        else
        {
            // Start forward playback
            _isPlayingForward = true;
            PingPongMedia.Position = TimeSpan.Zero;
            PingPongMedia.Play();
        }
    }

    private void StartReversePlayback()
    {
        _reverseTimer = new DispatcherTimer();
        _reverseTimer.Interval = TimeSpan.FromMilliseconds(50);
        _reverseTimer.Tick += ReverseTimer_Tick;
        _reverseTimer.Start();
    }

    private void ReverseTimer_Tick(object? sender, EventArgs e)
    {
        if (PingPongMedia.Position > TimeSpan.Zero)
        {
            PingPongMedia.Position -= TimeSpan.FromMilliseconds(100);
        }
        else
        {
            _reverseTimer?.Stop();
            PingPongMedia.Position = TimeSpan.Zero;
            _isPlayingForward = true;
            PingPongMedia.Play();
        }
    }
}