using System.Windows;
using CaffeinePro.Classes;
using CaffeinePro.Localization;
using CaffeinePro.Windows;

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

        /// <summary>
        /// Asks the user for a new blackout shortcut and stores it. Changing the setting is enough
        /// for the shortcut to be re-registered with Windows - HotKeyService watches for it.
        /// </summary>
        private void ChangeBlackoutHotKey_Click(object sender, RoutedEventArgs e)
        {
            var hotKey = HotKeyRecorderWindow.AskForHotKey(App.CurrentApp.AppSettings.BlackoutHotKey);

            if (hotKey is not null)
            {
                App.CurrentApp.AppSettings.BlackoutHotKey = hotKey.Value;
            }
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
            if (App.CurrentApp.AppSettings.StartActive == true
                && App.CurrentApp.AppSettings.StartupAwakeness.IsIndefinite
                && App.CurrentApp.KeepAwakeService.Awakeness.RelativeSpan != App.CurrentApp.AppSettings.StartupAwakeness.RelativeSpan)
            {
                if (
                    Dialogs.Show(
                        LocalizationService.Get("Startup_UpdateAwakeness_Message") +
                        Environment.NewLine +
                        App.CurrentApp.KeepAwakeService.Awakeness.GetAwakenessDescription(),
                        LocalizationService.Get("Startup_UpdateAwakeness_Title"),
                        MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    App.CurrentApp.AppSettings.StartupAwakeness = App.CurrentApp.KeepAwakeService.Awakeness;    
                }
            }
        }
    }
}
