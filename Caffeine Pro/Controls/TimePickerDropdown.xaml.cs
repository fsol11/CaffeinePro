using System.Windows;
using System.Windows.Controls;
using TimePicker;

namespace Caffeine_Pro.Controls
{
    /// <summary>
    /// Interaction logic for TimePickerDropdown.xaml
    /// </summary>
    public partial class TimePickerDropdown
    {
        public int Hours
        {
            get => (int)GetValue(HoursProperty);
            set => SetValue(HoursProperty, value);
        }

        public static readonly DependencyProperty HoursProperty =
            DependencyProperty.Register(nameof(Hours), typeof(int), typeof(TimePickerDropdown), new PropertyMetadata(0, TimeComponentsChangedCallback));

        private void SetTime()
        {
            if (Time.Hour != Hours || Time.Minute != Minutes || Time.HalfDaySign != AMPM)
            {
                Time = new AnalogTime(Hours, Minutes, AMPM);
            }
        }

        public int Minutes
        {
            get => (int)GetValue(MinutesProperty);
            set => SetValue(MinutesProperty, value);
        }

        public static readonly DependencyProperty MinutesProperty =
            DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(TimePickerDropdown), new PropertyMetadata(0, TimeComponentsChangedCallback));


        public AnalogTime Time
        {
            get => (AnalogTime)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(nameof(Time), typeof(AnalogTime), typeof(TimePickerDropdown), new PropertyMetadata(AnalogTime.Default, TimeChangedCallback));

        private static void TimeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tp = (TimePickerDropdown)d;
            if (tp.Hours != tp.Time.Hour)
            {
                tp.Hours = tp.Time.Hour;
            }
            if (tp.Minutes != tp.Time.Minute)
            {
                tp.Minutes = tp.Time.Minute;
            }
            if (tp.AMPM != tp.Time.HalfDaySign)
            {
                tp.AMPM = tp.Time.HalfDaySign;
            }
        }

        public HalfDaySign AMPM
        {
            get => (HalfDaySign)GetValue(AMPMProperty);
            set => SetValue(AMPMProperty, value);
        }

        public static readonly DependencyProperty AMPMProperty =
            DependencyProperty.Register(nameof(AMPM), typeof(HalfDaySign), typeof(TimePickerDropdown), new PropertyMetadata(HalfDaySign.AM, TimeComponentsChangedCallback));

        private static void TimeComponentsChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TimePickerDropdown)!.SetTime();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lv = (ListView)sender;
            lv.ScrollIntoView(lv.SelectedItem ?? lv.Items[0]!);
        }
    }
}
