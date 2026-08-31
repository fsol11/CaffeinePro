using System.Windows;
using System.Windows.Controls;
using CaffeinePro.Classes;
using CaffeinePro.Localization;
using CaffeinePro.Windows;

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
        => ((AwakenessViewControl)d).UpdateShouldShowDate();

    /// <summary>
    /// Outside the startup options the end date is always shown - it is empty for today anyway, and
    /// leaving it out hid the "Tomorrow" on a duration long enough to run past midnight. Inside the
    /// startup options only an indefinite awakeness has a date to show ("Indefinitely"), because the
    /// app is not running yet and any concrete date would be meaningless.
    /// </summary>
    private void UpdateShouldShowDate()
        => ShouldShowDate = AwakenessValue.IsIndefinite || !InStartupOptions;

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
    {
        var control = (AwakenessViewControl)d;
        var inStartupOptions = (bool)e.NewValue;

        control.RelativeTimeSlider.InStartupOptions = inStartupOptions;

        // Recomputed here as well: this property and AwakenessValue are both set from XAML, in no
        // guaranteed order, and ShouldShowDate depends on the two of them.
        control.UpdateShouldShowDate();

        // Only collapsed here, never shown: the section is authored visible so that the false case
        // works too - setting the property to the value it already has raises no change callback.
        control.DefaultAwakenessSection.Visibility = inStartupOptions ? Visibility.Collapsed : Visibility.Visible;
    }



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
        AwakenessValue = RelativeTimeSlider switch
        {
            // The top of the slider's range means "indefinitely", not "for 24 hours".
            { IsIndefinite: true } => new Awakeness(Awakeness.AwakenessTypes.Absolute, TimeSpan.MaxValue),
            { IsForSelection: true } slider => new Awakeness(Awakeness.AwakenessTypes.Relative, slider.TotalMinutes),
            _ => new Awakeness(Awakeness.AwakenessTypes.Absolute, GetSelectedTimeOfDay())
        };

        RaiseNewAwakenessSelected();
    }

    /// <summary>
    /// Adopts the default (startup) awakeness. It is renewed first: the end time stored in the
    /// settings was calculated when they were loaded, so a relative default ("for 2 hours") has to
    /// be recounted from now.
    /// </summary>
    private void SetToDefaultButton_OnClick(object sender, RoutedEventArgs e)
    {
        AwakenessValue = Awakeness.RenewDateTime(App.CurrentApp.AppSettings.StartupAwakeness);

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

        // The reading direction and font are pushed in by hand as well as bound. Shown as a
        // drop-down's flyout, this menu is hosted in a popup of its own, and the values it was given
        // when that popup was first built outlive a later language change - which left the picker
        // mirrored, in the previous language's font, after switching away from Arabic or Farsi.
        // SetCurrentValue rather than a plain assignment, so the bindings stay attached.
        SetTimeFlyout.SetCurrentValue(FlowDirectionProperty, LocalizationService.Instance.FlowDirection);
        SetTimeFlyout.SetCurrentValue(FontFamilyProperty, LocalizationService.Instance.FontFamily);

        // Everything in the flyout is re-read on the way in. Two kinds of text here go stale while
        // it is closed and nothing would otherwise notice: the default's "until ..." hint, which is
        // counted from now rather than from the setting, and the durations and clock times, which a
        // converter produces from sources that do not change when the language does.
        UiRefresher.Refresh(SetTimeFlyout);
    }
}