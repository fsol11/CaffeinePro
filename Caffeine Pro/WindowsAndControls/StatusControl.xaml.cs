using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.WindowsAndControls
{
    /// <summary>
    /// Interaction logic for StatusControl.xaml
    /// </summary>
    public partial class StatusControl : UserControl, INotifyPropertyChanged
    {
        public event EventHandler? OnPlus15;
        public event EventHandler? OnMinus15;

        public StatusControl()
        {
            InitializeComponent();
            SetInactive();
            DataContext = this;
        }

        public void SetActive(DateTime until)
        {
            ActiveStatus.Visibility = Visibility.Visible;
            InactiveStats.Visibility = Visibility.Collapsed;
            if (until == DateTime.MaxValue)
            {
                ActiveUntilText.Visibility = Visibility.Collapsed;
            }
            else
            {
                ActiveUntilText.Text = Routines.GetTimeString(until);
                ActiveUntilText.Visibility = Visibility.Visible;
            }
        }

        public void SetInactive()
        {
            ActiveStatus.Visibility = Visibility.Collapsed;
            InactiveStats.Visibility = Visibility.Visible;
            ActiveUntilText.Visibility = Visibility.Collapsed;
        }

        private void OnPlus15Btn(object sender, RoutedEventArgs e)
        {
            OnPlus15?.Invoke(this, EventArgs.Empty);
        }

        private void OnMinus15Btn(object sender, RoutedEventArgs e)
        {
            OnMinus15?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
