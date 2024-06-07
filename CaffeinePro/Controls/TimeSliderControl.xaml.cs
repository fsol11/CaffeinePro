using System.Windows;
using System.Windows.Controls;

namespace CaffeinePro.Controls;

/// <summary>
/// Interaction logic for TimePickerSlider.xaml
/// </summary>
public partial class TimeSliderControl
{
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(TimeSpan), typeof(TimeSliderControl));

    public static readonly DependencyProperty NoSpecificDateProperty = DependencyProperty.Register(nameof(NoSpecificDate), typeof(bool), typeof(TimeSliderControl), new PropertyMetadata(default(bool)));

    public TimeSliderControl()
    {
        InitializeComponent();
        Time  = TimeSpan.FromHours(8); // <- Default Value
    }

    public TimeSpan Time
    {
        get => (TimeSpan)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    } 

    public bool NoSpecificDate
    {
        get => (bool)GetValue(NoSpecificDateProperty);
        set => SetValue(NoSpecificDateProperty, value);
    }

    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        Time = TimeSpan.FromMinutes(int.Parse((menu.Tag as string)!));
    }

    private void MinutesSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Math.Abs(Time.TotalMinutes - e.NewValue) > .01)
        {
            Time = TimeSpan.FromMinutes((int)e.NewValue);
        }
    }
}

