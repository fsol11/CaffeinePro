using System.Windows;
using System.Windows.Controls;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.Controls;

/// <summary>
/// Interaction logic for AwakenessControl.xaml
/// </summary>
public sealed partial class AwakenessControl
{
    public static readonly DependencyProperty AwakenessValueProperty = DependencyProperty.Register(
        nameof(AwakenessValue),
        typeof(Awakeness),
        typeof(AwakenessControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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

    public static readonly DependencyProperty RelativeTimeDisabledProperty = DependencyProperty.Register(
        nameof(RelativeTimeDisabled),
        typeof(bool),
        typeof(AwakenessControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool RelativeTimeDisabled
    {
        get => (bool)GetValue(RelativeTimeDisabledProperty);
        set => SetValue(RelativeTimeDisabledProperty, value);
    }

    public AwakenessControl()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void OnPlus15Btn(object sender, RoutedEventArgs e)
    {
        AwakenessValue.Plus15Minutes();
    }

    private void OnMinus15Btn(object sender, RoutedEventArgs e)
    {
        AwakenessValue.Minus15Minutes();
    }

    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        if (menu.Tag == null)
        {
            AwakenessValue = Awakeness.Indefinite;
        }
        else
        {
            AwakenessValue = RelativeTimeDisabled 
                ? new Awakeness(Routines.GetDateTimeFromTimeSpan(TimeSpan.FromMinutes(int.Parse((menu.Tag as string)!)))) 
                : new Awakeness(TimeSpan.FromMinutes(int.Parse((menu.Tag as string)!)));
        }
    }

    private void AbsoluteTimeApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        AwakenessValue = new Awakeness(AbsoluteTimePicker.Time.ToDateTime());
        SelectionMenu.IsOpen = false;
    }

    private void RelativeTimeApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        AwakenessValue = 
            RelativeTimeDisabled
            ? new Awakeness(DateTime.Now.Add(RelativeTimeSlider.Time.ToTimeSpan())) 
            : new Awakeness(RelativeTimeSlider.Time.ToTimeSpan());
        SelectionMenu.IsOpen = false;
    }

    private void AbsoluteTimeOnClick_ActivateUntil(object sender, RoutedEventArgs e)
    {
        var text = (string)((MenuItem)sender).Header;
        var hours = int.Parse(text[..2].Trim());
        var minutes = int.Parse(text[3..5]);
        var halfDaySign = text[5..].Trim();
        if (halfDaySign == "PM")
        {
            hours += 12;
        }

        AwakenessValue = new Awakeness(Routines.GetDateTimeFromTimeSpan(new TimeSpan(hours, minutes, 0)));
    }

    private void ResetOptions(object sender, RoutedEventArgs e)
    {
        AwakenessValue.Options.Reset();
    }

    private void UntilMenuOpened(object sender, RoutedEventArgs e)
    {
        AbsoluteTimePicker.Time = AwakenessValue.UntilDateTime.TimeOfDay;
    }
}
