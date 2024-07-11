using System.Windows;
using CaffeinePro.Classes;
using CaffeinePro.Services;

namespace CaffeinePro.Controls
{
    /// <summary>
    /// Interaction logic for StatusControl.xaml
    /// </summary>
    public partial class StatusControl
    {
        public StatusControl()
        {
            InitializeComponent();
        }

        private void OnActivate(object sender, RoutedEventArgs e)
        {
            App.CurrentApp.KeepAwakeService.Activate();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            App.CurrentApp.KeepAwakeService.Deactivate();
        }

        private void OnNewAwakenessSelected(object? sender, Awakeness aw)
        {
            App.CurrentApp.KeepAwakeService.Activate(aw);
            App.CurrentApp.TrayIcon!.ContextMenu!.IsOpen = false;
        }

        private void SetToStartupValue_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentApp.KeepAwakeService.Awakeness = App.CurrentApp.AppSettings.StartupAwakeness;
        }
    }
}
