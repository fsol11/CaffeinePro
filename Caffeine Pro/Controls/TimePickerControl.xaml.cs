using System.Windows;
using Caffeine_Pro.Classes;

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
    }
}
