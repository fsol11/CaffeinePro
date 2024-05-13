using System.ComponentModel;
using System.Windows;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.Controls
{
    /// <summary>
    /// Interaction logic for StatusControl.xaml
    /// </summary>
    public partial class StatusControl 
    {
        public static readonly RoutedEvent AwakenessChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(AwakenessChanged), // Event name
            RoutingStrategy.Bubble, // Routing strategy
            typeof(RoutedEventHandler), // Event handler type
            typeof(StatusControl)); // Owner type

        public event RoutedEventHandler AwakenessChanged
        {
            add => AddHandler(AwakenessChangedEvent, value);
            remove => RemoveHandler(AwakenessChangedEvent, value);
        }

        public static readonly DependencyProperty AwakenessProperty = DependencyProperty.Register(
            nameof(Awakeness),
            typeof(Awakeness),
            typeof(StatusControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Awakeness Awakeness
        {
            get
            {
                var a = (Awakeness)GetValue(AwakenessProperty);
                if (a != null)
                {
                    return a;
                }

                a = new Awakeness();
                SetValue(AwakenessProperty, a);

                return a;
            }
            set
            {
                var a = (Awakeness)GetValue(AwakenessProperty);
                if(a != null)
                {
                    a.PropertyChanged -= OnAwakenessPropertyChanged;
                }

                SetValue(AwakenessProperty, value);
                value.PropertyChanged += OnAwakenessPropertyChanged;
            }
        }

        private void OnAwakenessPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(AwakenessChangedEvent));
        }


        public StatusControl()
        {
            InitializeComponent();
        }

        private void OnActivate(object sender, RoutedEventArgs e)
        {
            App.CurrentApp.KeepAwakeService.Activate();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            App.CurrentApp.KeepAwakeService.Deactivate();
        }
    }
}
