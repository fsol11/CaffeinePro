using System.Windows;
using System.Windows.Controls;

namespace TimePicker;

/// <summary>
/// A slider like time picker control https://github.com/roy-t/TimePicker 
/// </summary>
public class TimeSlider : Control
{
    static TimeSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeSlider),
            new FrameworkPropertyMetadata(typeof(TimeSlider)));
    }

    public DigitalTime Time
    {
        get => (DigitalTime)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(DigitalTime), typeof(TimeSlider), new PropertyMetadata(new DigitalTime(12, 0), TimeChanged));

    public DigitalTime MinTime
    {
        get => (DigitalTime)GetValue(MinTimeProperty);
        set => SetValue(MinTimeProperty, value);
    }

    public static readonly DependencyProperty MinTimeProperty =
        DependencyProperty.Register(nameof(MinTime), typeof(DigitalTime), typeof(TimeSlider), new PropertyMetadata(new DigitalTime(9, 0), TimeChanged));

    public DigitalTime MaxTime
    {
        get => (DigitalTime)GetValue(MaxTimeProperty);
        set => SetValue(MaxTimeProperty, value);
    }

    public static readonly DependencyProperty MaxTimeProperty =
        DependencyProperty.Register(nameof(MaxTime), typeof(DigitalTime), typeof(TimeSlider), new PropertyMetadata(new DigitalTime(21, 0), TimeChanged));


    private static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        if (d is not TimeSlider slider)
        {
            return;
        }

        if (slider.MaxTime < slider.MinTime)
        {
            slider.MaxTime = slider.MinTime;
        }

        if (slider.Time > slider.MaxTime)
        {
            slider.Time = slider.MaxTime;
        }

        if (slider.Time < slider.MinTime)
        {
            slider.Time = slider.MinTime;
        }
    }
}
