using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.Controls
{
    /// <summary>
    /// Interaction logic for StartupOptionsControl.xaml
    /// </summary>
    public partial class StartupOptionsControl : UserControl, INotifyPropertyChanged
    {
        private Awakeness _startupAwakeness = new();
        private bool _startupActivated;

        public Awakeness StartupAwakeness
        {
            get => _startupAwakeness;
            set => SetField(ref _startupAwakeness, value);
        }

        public bool StartupActivated
        {
            get => _startupActivated;
            set => SetField(ref _startupActivated, value);
        }

        public StartupOptionsControl()
        {
            InitializeComponent();
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
