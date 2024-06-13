using System.Windows;
using System.Windows.Controls;
using CaffeinePro.Classes;
using Timer = System.Timers.Timer;

namespace CaffeinePro.Controls;

public partial class TimePickerControl
{
    public event EventHandler<TimeSpan>? NewTimeSelected;
    private readonly Timer _timer = new(60000);

    public TimePickerControl()
    {
        InitializeComponent();
        _timer.Elapsed += (_, _) => Dispatcher.BeginInvoke(() => Now = DateTime.Now);
        _timer.Start();
        Now = DateTime.Now;
    }

    public static readonly DependencyProperty NowProperty =
        DependencyProperty.Register(
            nameof(Now),
            typeof(DateTime),
            typeof(TimePickerControl),
            new FrameworkPropertyMetadata(DateTime.MaxValue));

    public static readonly DependencyProperty InStartupOptionsProperty =
        DependencyProperty.Register(nameof(InStartupOptions), typeof(bool),
            typeof(TimePickerControl),
            new PropertyMetadata(default(bool)));

    public DateTime Now
    {
        get => (DateTime)GetValue(NowProperty);
        set => SetValue(NowProperty, value);
    }

    public bool InStartupOptions
    {
        get => (bool)GetValue(InStartupOptionsProperty);
        set => SetValue(InStartupOptionsProperty, value);
    }

    private void TimeButton_Click(object sender, RoutedEventArgs e)
    {
        NewTimeSelected?.Invoke(this, Routines.ContentToTimeSpan(sender));
        ((Parent as MenuItem)!.Parent as MenuItem)!.IsSubmenuOpen = false;
        e.Handled = true;
    }
}