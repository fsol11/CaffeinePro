using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace Caffeine_Pro.WindowsAndControls;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class AboutWindow : Window
{
    private static AboutWindow? _aboutWindow;
    public static string AppName => Assembly.GetExecutingAssembly().GetName().Name!.ToString();
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    /// <summary>
    /// A static function to create and show the About window. If the window is already open,
    /// it will be brought to the front.
    /// </summary>
    public static void ShowIt()
    {
        if (_aboutWindow is { IsLoaded: true })
        {
            _aboutWindow.Show();
            _aboutWindow.Activate();
        }
        else
        {
            _aboutWindow = new AboutWindow();
            _aboutWindow.Show();
        }
    }

    /// <summary>
    /// Closes the About window if it is open
    /// </summary>
    public static void CloseIt()
    {
        if (_aboutWindow is { IsLoaded: true }) _aboutWindow.Close();
    }

    /// <summary>
    /// Initializes the About window and sets up the commandline usage information text box
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Close the window when close button is pressed
    /// </summary>
    private void CloseBtnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Handle Hyperlink click. The URL that is set in Hyperlink will be opened in the default browser
    /// </summary>
    private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        var uri = ((Hyperlink)sender).NavigateUri;
        Process.Start(new ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });
    }


}
