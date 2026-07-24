using System.Windows;
using CaffeinePro.Classes;

namespace CaffeinePro.Controls;

/// <summary>
/// Interaction logic for AwakenessControl.xaml
/// </summary>
public sealed partial class AwakenessViewControl
{
    public event EventHandler<Awakeness>? NewAwakenessSelected;
    
    public static readonly DependencyProperty AwakenessValueProperty = DependencyProperty.Register(
        nameof(AwakenessValue),
        typeof(Awakeness),
        typeof(AwakenessViewControl),
        new FrameworkPropertyMetadata(default(Awakeness), ValueChanged));

    private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var awakeness = ((Awakeness)e.NewValue);
        var control = (AwakenessViewControl)d;
        control.ShouldShowDate = awakeness.IsIndefinite || (!awakeness.IsRelative && !control.InStartupOptions);
    }

    public Awakeness AwakenessValue
    {
        get
        {
            var a = (Awakeness)GetValue(AwakenessValueProperty);
            if (a != null)
            {
                return a;
            }

            a = new Awakeness();
            SetValue(AwakenessValueProperty, a);

            return a;
        }
        set => SetValue(AwakenessValueProperty, value);
    }

    public static readonly DependencyProperty InStartupOptionsProperty = DependencyProperty.Register(
        nameof(InStartupOptions),
        typeof(bool),
        typeof(AwakenessViewControl),
        new FrameworkPropertyMetadata(false));
    

    public static readonly DependencyProperty ShouldShowDateProperty =
        DependencyProperty.Register(nameof(ShouldShowDate), typeof(bool), typeof(AwakenessViewControl),
            new PropertyMetadata(default(bool)));

    public bool InStartupOptions
    {
        get => (bool)GetValue(InStartupOptionsProperty);
        set => SetValue(InStartupOptionsProperty, value);
    }

    public bool ShouldShowDate
    {
        get => (bool)GetValue(ShouldShowDateProperty);
        set => SetValue(ShouldShowDateProperty, value);
    }

    public AwakenessViewControl()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void MenuItemIndefinitely_OnClick(object sender, RoutedEventArgs e)
    {
        AwakenessValue = new Awakeness(
            Awakeness.AwakenessTypes.Absolute,
            TimeSpan.MaxValue
        );

        NewAwakenessSelected?.Invoke(this, AwakenessValue);
    }

    private void OnNewTimeSelected(object? _, TimeSpan t)
    {
        var dt = Routines.GetDateTimeFromTimeSpan(t, Awakeness.AwakenessTypes.Absolute);
        var ts = Routines.GetRelativeTimeSpanFromDateTime(dt);

        RelativeTimeSlider.SetRelativeTime(ts);
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        AwakenessValue = new Awakeness(
            Awakeness.AwakenessTypes.Relative,
            RelativeTimeSlider.TotalMinutes);

        NewAwakenessSelected?.Invoke(this, AwakenessValue);
    }

    private void SetTimeMenuOpened(object sender, RoutedEventArgs e)
    {
        RelativeTimeSlider.SetRelativeTime(
            Routines.GetRelativeTimeSpanFromDateTime(App.CurrentApp.AppSettings.StartupAwakeness.EndDateTime));
    }
}