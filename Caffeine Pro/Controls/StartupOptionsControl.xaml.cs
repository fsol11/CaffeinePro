using System.Windows;

namespace Caffeine_Pro.Controls
{
    /// <summary>
    /// Interaction logic for StartupOptionsControl.xaml
    /// </summary>
    public partial class StartupOptionsControl
    {


        public StartupOptionsControl()
        {
            InitializeComponent();
        }

        private void SetToCurrentValue_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).AppSettings.StartupAwakeness = ((App)Application.Current).KeepAwakeService.Awakeness;
        }
    }
}
