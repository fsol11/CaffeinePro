using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.WindowsAndControls;

/// <summary>
/// Interaction logic for AwakenessControl.xaml
/// </summary>
public sealed partial class AwakenessControl : INotifyPropertyChanged
{
    public Awakeness Awakeness
    {
        get => _awakeness;
        set => SetField(ref _awakeness, value);
    }

    private bool _showPlusMinusButtons = true;
    private Awakeness _awakeness;

    public AwakenessControl(Awakeness awakeness)
    {
        _awakeness = awakeness;
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
        Awakeness.Plus15Minutes();
    }

    private void OnMinus15Btn(object sender, RoutedEventArgs e)
    {
        Awakeness.Minus15Minutes();
    }

    // INotifyPropertyChanged implementation ------------------------------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
