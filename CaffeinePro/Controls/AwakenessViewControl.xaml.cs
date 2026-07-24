using System.Windows;
using System.Windows.Controls;
using CaffeinePro.Classes;
using CaffeinePro.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
            TimeSpan.MaxValue,
            AwakenessValue.Options,
            AwakenessValue.AfterwardsAction
        );
        
        NewAwakenessSelected?.Invoke(this, AwakenessValue);
    }


    private void ResetOptions(object sender, RoutedEventArgs e)
    {
        AwakenessValue = new Awakeness(AwakenessValue.AwakenessType, AwakenessValue.RelativeSpan, new AwakenessOptions(), SessionAction.None);
    }

    private void ToggleAwakenessOptionMenu_Click(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag as string;
        AwakenessValue = tag switch
        {
            "InactiveWhenCpuBelowPercentage" => new Awakeness(AwakenessValue.AwakenessType,
                AwakenessValue.RelativeSpan,
                new AwakenessOptions(AwakenessValue.Options.InactiveWhenOnBattery,
                    !AwakenessValue.Options.InactiveWhenCpuBelowPercentage,
                    AwakenessValue.Options.CpuBelowPercentage), AwakenessValue.AfterwardsAction),
            "InactiveWhenOnBattery" => new Awakeness(AwakenessValue.AwakenessType, AwakenessValue.RelativeSpan,
                new AwakenessOptions(!AwakenessValue.Options.InactiveWhenOnBattery,
                    AwakenessValue.Options.InactiveWhenCpuBelowPercentage, AwakenessValue.Options.CpuBelowPercentage),
                AwakenessValue.AfterwardsAction),
            _ => AwakenessValue
        };
        e.Handled = true;
    }

    private void CpuPercentageChanged(object sender, KeyEventArgs keyEventArgs)
    {
        var numericBox = (NumericTextBox)sender;

        AwakenessValue = new Awakeness(AwakenessValue.AwakenessType,
            AwakenessValue.RelativeSpan,
            new AwakenessOptions(
                AwakenessValue.Options.InactiveWhenOnBattery,
                AwakenessValue.Options.InactiveWhenCpuBelowPercentage,
                numericBox.Number),
            AwakenessValue.AfterwardsAction);
    }

    private void AfterwardsAction_Click(object sender, RoutedEventArgs e)
    {
        if (((Control)sender).Tag is SessionAction action)
        {
            AwakenessValue =
                new Awakeness(
                    AwakenessValue.AwakenessType,
                    AwakenessValue.RelativeSpan,
                    AwakenessValue.Options,
                    action);
        }
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
            RelativeTimeSlider.TotalMinutes,
            AwakenessValue.Options,
            AwakenessValue.AfterwardsAction);

        NewAwakenessSelected?.Invoke(this, AwakenessValue);
    }

    private void SetTimeMenuOpened(object sender, RoutedEventArgs e)
    {
        RelativeTimeSlider.SetRelativeTime(
            Routines.GetRelativeTimeSpanFromDateTime(App.CurrentApp.AppSettings.StartupAwakeness.EndDateTime));
    }
}