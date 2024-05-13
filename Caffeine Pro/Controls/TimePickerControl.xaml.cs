using System.Windows;
using System.Windows.Controls;
using TimePicker;

namespace Caffeine_Pro.Controls
{
    /// <summary>
    /// Interaction logic for TimePickerControl.xaml
    /// </summary>
    public partial class TimePickerControl
    {
        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(nameof(Time), typeof(AnalogTime), typeof(TimePickerControl));

        public AnalogTime Time
        {
            get => (AnalogTime)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value );
        }

        public TimePickerControl()
        {
            InitializeComponent();
        }

        // ReSharper disable once IdentifierTypo
        private void AMFM_OnClick(object sender, RoutedEventArgs e)
        {
            //Time = new AnalogTime(Time.Hour, Time.Minute, Time.HalfDaySign == HalfDaySign.AM ? HalfDaySign.PM : HalfDaySign.AM);
        }
    }
}
