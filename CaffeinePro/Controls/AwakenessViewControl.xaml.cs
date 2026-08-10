using System.Windows;
using CaffeinePro.Classes;
using Windows.ApplicationModel.VoiceCommands;

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
        new FrameworkPropertyMetadata(false, InStartupOptionsChanged));

    // The slider lives inside the drop-down's ContextMenu, which is outside this control's inherited
    // DataContext, so the flag is pushed across instead of bound in XAML.
    private static void InStartupOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AwakenessViewControl)d).RelativeTimeSlider.InStartupOptions = (bool)e.NewValue;



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

    /// <summary>
    /// Announces the awakeness the user just picked and dismisses the picker.
    /// </summary>
    private void RaiseNewAwakenessSelected()
    {
        // Closed through the flyout itself rather than through DropDownButton.IsDropDownOpen: that
        // property only mirrors the menu (the menu pushes its state into it, not the other way
        // round), so setting it would leave the picker on screen.
        SetTimeFlyout.IsOpen = false;

        NewAwakenessSelected?.Invoke(this, AwakenessValue);
    }

    private void MenuItemIndefinitely_OnClick(object sender, RoutedEventArgs e)
    {
        AwakenessValue = new Awakeness(
            Awakeness.AwakenessTypes.Absolute,
            TimeSpan.MaxValue
        );

        RaiseNewAwakenessSelected();
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        AwakenessValue = RelativeTimeSlider.IsForSelection
            ? new Awakeness(Awakeness.AwakenessTypes.Relative, RelativeTimeSlider.TotalMinutes)
            : new Awakeness(Awakeness.AwakenessTypes.Absolute, GetSelectedTimeOfDay());

        RaiseNewAwakenessSelected();
    }

    /// <summary>
    /// Returns the clock time the slider currently points at. A time picked in the UNTIL section is
    /// kept absolute so the end time does not drift by however long the user takes to press Apply.
    /// </summary>
    private TimeSpan GetSelectedTimeOfDay()
    {
        var timeOfDay = Awakeness.GetNow().Add(RelativeTimeSlider.TotalMinutes).TimeOfDay;

        // Awakeness reads a zero span as "indefinite", so midnight is expressed as a full day.
        return timeOfDay == TimeSpan.Zero ? TimeSpan.FromDays(1) : timeOfDay;
    }

    private void SetTimeMenuOpened(object sender, RoutedEventArgs e)
    {
        // Seeded from the awakeness this control is bound to (the active one in the status bar, the
        // startup one in the startup options) rather than always from the startup awakeness.
        RelativeTimeSlider.SetRelativeTime(Routines.ToRelativeTime(AwakenessValue.EndDateTime));
    }
}