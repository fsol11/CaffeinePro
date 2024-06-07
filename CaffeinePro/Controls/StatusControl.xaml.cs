using System.Windows;
using CaffeinePro.Classes;

namespace CaffeinePro.Controls
{
    /// <summary>
    /// Interaction logic for StatusControl.xaml
    /// </summary>
    public partial class StatusControl 
    {
        public static readonly DependencyProperty AwakenessProperty = DependencyProperty.Register(
            nameof(Awakeness),
            typeof(Awakeness),
            typeof(StatusControl),
            new FrameworkPropertyMetadata(null));

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
            set => SetValue(AwakenessProperty, value);
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
