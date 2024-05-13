using System.Windows;
using System.Windows.Controls;
using TimePicker;

namespace Caffeine_Pro.Controls;

/// <summary>
/// Interaction logic for TimePickerSlider.xaml
/// </summary>
public partial class TimeSliderControl 
{
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(AnalogTime), typeof(TimeSliderControl));

    public AnalogTime Time
    {
        get => (AnalogTime)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        Time = AnalogTime.FromMinutes(int.Parse((menu.Tag as string)!));
    }
}

