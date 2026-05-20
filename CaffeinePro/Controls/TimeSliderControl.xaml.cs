using System.Windows;
using System.Windows.Controls;

namespace CaffeinePro.Controls;

/// <summary>
/// Interaction logic for TimePickerSlider.xaml
/// </summary>
public partial class TimeSliderControl
{
    public static string[] PredefinedTimes1 { get; }= ["15", "30", "45"];
    public static string[] PredefinedTimes2 { get; } = ["60", "120", "180", "240", "300", "360", "420", "480", "540", "600", "720"];
    public static string[] PredefinedTimes3 { get; } = ["90", "150", "210", "270", "330", "390", "450", "510", "570", "630", "690"];

    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(TimeSpan), typeof(TimeSliderControl));

    public static readonly DependencyProperty InStartupOptionsProperty = DependencyProperty.Register(nameof(InStartupOptions), typeof(bool), typeof(TimeSliderControl), new PropertyMetadata(default(bool)));

    public event EventHandler? OnSelected;

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

    public bool InStartupOptions
    {
        get => (bool)GetValue(InStartupOptionsProperty);
        set => SetValue(InStartupOptionsProperty, value);
    }

    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        Time = TimeSpan.FromMinutes(int.Parse((menu.Tag as string)!));
        OnSelected?.Invoke(this, EventArgs.Empty);
    }

    private void MinutesSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Math.Abs(Time.TotalMinutes - e.NewValue) > .01)
        {
            Time = TimeSpan.FromMinutes((int)e.NewValue);
        }
    }
}

