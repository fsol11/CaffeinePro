using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Caffeine_Pro.WindowsAndControls;

/// <summary>
/// Interaction logic for StatusControl.xaml
/// </summary>
public partial class StatusControl : INotifyPropertyChanged
{
    private bool _showPlusMinusButtons = true;

    public event EventHandler? OnPlus15;
    public event EventHandler? OnMinus15;

    public StatusControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    public bool ShowPlusMinusButtons
    {
        get => _showPlusMinusButtons;
        set => SetField(ref _showPlusMinusButtons, value);
    }

    private void OnPlus15Btn(object sender, RoutedEventArgs e)
    {
        OnPlus15?.Invoke(this, EventArgs.Empty);
    }

    private void OnMinus15Btn(object sender, RoutedEventArgs e)
    {
        OnMinus15?.Invoke(this, EventArgs.Empty);
    }

    // INotifyPropertyChanged implementation ------------------------------------------------
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
