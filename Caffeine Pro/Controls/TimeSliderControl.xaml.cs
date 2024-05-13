using System.Windows;
using System.Windows.Controls;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.Controls;

/// <summary>
/// Interaction logic for TimePickerSlider.xaml
/// </summary>
public partial class TimeSliderControl
{
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(AnalogTime), typeof(TimeSliderControl));

    public TimeSliderControl()
    {
        InitializeComponent();
    }

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

    private void MinutesSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Math.Abs(Time.TotalMinutes - e.NewValue) > .5)
        {
            Time = AnalogTime.FromMinutes((int)e.NewValue);
        }
    }
}

