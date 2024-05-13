using System.Windows;
using System.Windows.Controls;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.Controls
{
    /// <summary>
    /// Interaction logic for TimePickerDropdown.xaml
    /// </summary>
    public partial class TimePickerDropdown
    {
        private bool _updating = false;

        public TimePickerDropdown()
        {
            InitializeComponent();
        }


        private static void TimeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tp = (TimePickerDropdown)d;
            if (tp._updating)
            {
                return;
            }

            tp._updating = true;
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
            tp._updating = false;
        }

        private static void TimeComponentsChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tp = (TimePickerDropdown)d;
            if (tp._updating)
            {
                return;
            }

            tp._updating = true;
            if (tp.Time.Hour != tp.Hours || tp.Time.Minute != tp.Minutes || tp.Time.HalfDaySign != tp.AMPM)
            {
                tp.Time = new AnalogTime(tp.Hours, tp.Minutes, tp.AMPM);
            }
            tp._updating = false;
        }

        public int Hours
        {
            get => (int)GetValue(HoursProperty);
            set => SetValue(HoursProperty, value);
        }

        public static readonly DependencyProperty HoursProperty =
            DependencyProperty.Register(nameof(Hours), typeof(int), typeof(TimePickerDropdown),
                new PropertyMetadata(0, TimeComponentsChangedCallback));

        public int Minutes
        {
            get => (int)GetValue(MinutesProperty);
            set => SetValue(MinutesProperty, value);
        }

        public static readonly DependencyProperty MinutesProperty =
            DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(TimePickerDropdown),
                new PropertyMetadata(0, TimeComponentsChangedCallback));


        public AnalogTime Time
        {
            get => (AnalogTime)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(nameof(Time), typeof(AnalogTime), typeof(TimePickerDropdown),
                new PropertyMetadata(AnalogTime.Default, TimeChangedCallback));


        public HalfDaySign AMPM
        {
            get => (HalfDaySign)GetValue(AMPMProperty);
            set => SetValue(AMPMProperty, value);
        }

        public static readonly DependencyProperty AMPMProperty =
            DependencyProperty.Register(nameof(AMPM), typeof(HalfDaySign), typeof(TimePickerDropdown),
                new PropertyMetadata(HalfDaySign.AM, TimeComponentsChangedCallback));

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lv = (ListView)sender;
            lv.ScrollIntoView(lv.SelectedItem ?? lv.Items[0]!);
        }

        private void PM_Click(object sender, RoutedEventArgs e)
        {
            AMPM = HalfDaySign.PM;
        }
        
        private void AM_Click(object sender, RoutedEventArgs e)
        {
            AMPM = HalfDaySign.AM;
        }
    }
}