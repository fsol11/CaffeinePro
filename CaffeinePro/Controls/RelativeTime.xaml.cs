using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CaffeinePro.Classes;

namespace CaffeinePro.Controls;

/// <summary>
/// Interaction logic for RelativeTime.xaml
/// </summary>
public partial class RelativeTime : UserControl
{
    public static readonly DependencyProperty MinutesProperty =
        DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(RelativeTime), new PropertyMetadata(0));

    public int Minutes
    {
        get => (int)GetValue(MinutesProperty);
        set => SetValue(MinutesProperty, value);
    }

    public string DisplayTime => Routines.GetDateTimeString(Awakeness.GetNow().Add(new TimeSpan(0,0,Minutes, 0)));

    public RelativeTime()
    {
        InitializeComponent();
    }
}