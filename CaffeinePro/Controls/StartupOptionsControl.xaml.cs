using System.Windows;

namespace CaffeinePro.Controls
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
            StartupAwakenessControl.AwakenessValue = ((App)Application.Current).KeepAwakeService.Awakeness;
        }

        private void StartActive_OnClick(object sender, RoutedEventArgs e)
        {
            CheckAwakenessUpdate();
        }
        
        private void CheckAwakenessUpdate()
        {
            if (App.CurrentApp.AppSettings.StartActive 
                && App.CurrentApp.AppSettings.StartupAwakeness.IsIndefinite
                && App.CurrentApp.KeepAwakeService.Awakeness != App.CurrentApp.AppSettings.StartupAwakeness)
            {
                if (
                    MessageBox.Show(
                        "Do you want to update startup awakeness to:\r\n" +
                        App.CurrentApp.KeepAwakeService.Awakeness.GetAwakenessDescription(),
                        "Update Startup Awakeness", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    App.CurrentApp.AppSettings.StartupAwakeness = App.CurrentApp.KeepAwakeService.Awakeness;    
                }
            }
        }
    }
}
