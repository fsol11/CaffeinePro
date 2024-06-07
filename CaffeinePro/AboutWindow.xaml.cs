using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using CaffeinePro.Classes;

namespace CaffeinePro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class AboutWindow
{
    private static AboutWindow? _window;
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
}