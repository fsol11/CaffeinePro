using System.Windows.Input;
using CaffeinePro.Classes;

namespace CaffeinePro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class NotificationWindow
{
    /// <summary>
    /// Initializes the About window and sets up the commandline usage information text box
    /// </summary>
    public NotificationWindow(Awakeness aw) 
    {
        InitializeComponent();
        TxtAwakeness.Text = aw.GetAwakenessDescription().Trim();
    }


    private void NotificationWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}