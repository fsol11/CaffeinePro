using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caffeine_Pro.Classes;
using static Caffeine_Pro.Classes.ClockMath;

namespace Caffeine_Pro.Controls;


/// <summary>
/// This is a modified version of this control:
/// Clock-like TimePicker control https://github.com/roy-t/TimePicker    
/// </summary>
public class ClockControl : Control
{
    public const double HourTickRatio = 0.20;
    public const double MinuteTickRatio = 0.10;
    public const double HourIndicatorRatio = 0.70;
    public const double MinuteIndicatorRatio = 0.95;

    private readonly ClockControlInputController _inputController;

    public ClockControl()
    {
        _inputController = new ClockControlInputController(this);
    }

    ~ClockControl()
    {
        _inputController.Dispose();
    }

    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(AnalogTime), typeof(ClockControl), new PropertyMetadata(new AnalogTime(), TimeChanged));

    public AnalogTime Time
    {
        get => (AnalogTime)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public static readonly DependencyProperty HourBrushProperty =
        DependencyProperty.Register(nameof(HourBrush), typeof(Brush), typeof(ClockControl), new PropertyMetadata(Brushes.Black));


    public Brush HourBrush
    {
        get => (Brush)GetValue(HourBrushProperty);
        set => SetValue(HourBrushProperty, value);
    }

    public static readonly DependencyProperty HourThicknessProperty =
        DependencyProperty.Register(nameof(HourThickness), typeof(double), typeof(ClockControl), new PropertyMetadata(5.0));

    public double HourThickness
    {
        get => (double)GetValue(HourThicknessProperty);
        set => SetValue(HourThicknessProperty, value);
    }

    public static readonly DependencyProperty MinuteBrushProperty =
        DependencyProperty.Register(nameof(MinuteBrush), typeof(Brush), typeof(ClockControl), new PropertyMetadata(Brushes.DarkBlue));

    public Brush MinuteBrush
    {
        get => (Brush)GetValue(MinuteBrushProperty);
        set => SetValue(MinuteBrushProperty, value);
    }

    public static readonly DependencyProperty MinuteThicknessProperty =
        DependencyProperty.Register(nameof(MinuteThickness), typeof(double), typeof(ClockControl), new PropertyMetadata(3.0));

    public double MinuteThickness
    {
        get => (double)GetValue(MinuteThicknessProperty);
        set => SetValue(MinuteThicknessProperty, value);
    }

    public static readonly DependencyProperty HourTickBrushProperty =
        DependencyProperty.Register(nameof(HourTickBrush), typeof(Brush), typeof(ClockControl), new PropertyMetadata(Brushes.Gray));

    public Brush HourTickBrush
    {
        get => (Brush)GetValue(HourTickBrushProperty);
        set => SetValue(HourTickBrushProperty, value);
    }

    public static readonly DependencyProperty HourTickThicknessProperty =
        DependencyProperty.Register(nameof(HourTickThickness), typeof(double), typeof(ClockControl), new PropertyMetadata(2.0));

    public double HourTickThickness
    {
        get => (double)GetValue(HourTickThicknessProperty);
        set => SetValue(HourTickThicknessProperty, value);
    }

    public static readonly DependencyProperty MinuteTickBrushProperty =
        DependencyProperty.Register(nameof(MinuteTickBrush), typeof(Brush), typeof(ClockControl), new PropertyMetadata(Brushes.DarkGray));

    public Brush MinuteTickBrush
    {
        get => (Brush)GetValue(MinuteTickBrushProperty);
        set => SetValue(MinuteTickBrushProperty, value);
    }

    public static readonly DependencyProperty MinuteTickThicknessProperty =
        DependencyProperty.Register(nameof(MinuteTickThickness), typeof(double), typeof(ClockControl), new PropertyMetadata(2.0));

    public double MinuteTickThickness
    {
        get => (double)GetValue(MinuteTickThicknessProperty);
        set => SetValue(MinuteTickThicknessProperty, value);
    }

    private static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        if (d is ClockControl timePicker)
        {
            timePicker.InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var width = RenderSize.Width - Padding.Left - Padding.Right;
        //var width = ActualWidth;
        var height = RenderSize.Height - Padding.Bottom-Padding.Top;
        var radius = (Math.Min(width, height) - BorderThickness.Left) / 2.0;
        var center = new Point(Padding.Left + width / 2.0, Padding.Top + height / 2.0);

        RenderBackground(drawingContext, width, height);

        RenderBorder(drawingContext, radius, center);

        RenderHourTicks(drawingContext, radius, center);
        RenderMinuteTicks(drawingContext, radius, center);

        RenderHour(drawingContext, radius, center);
        RenderMinute(drawingContext, radius, center);

        base.OnRender(drawingContext);
    }


    private static void RenderBackground(DrawingContext drawingContext, double width, double height)
    {
        // Always draw a transparent rectangle for hit tests
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
    }

    private void RenderMinute(DrawingContext drawingContext, double radius, Point center)
    {
        var pen = new Pen(MinuteBrush, MinuteThickness);

        drawingContext.DrawEllipse(MinuteBrush, pen, center, MinuteThickness * 1, MinuteThickness * 1);
        var points = LineOnCircle((Math.PI * 2.0 * Time.Minute / 60.0) - Math.PI / 2.0, center, 0, radius * MinuteIndicatorRatio);
        drawingContext.DrawLine(pen, points[0], points[1]);
    }

    private void RenderHour(DrawingContext drawingContext, double radius, Point center)
    {
        var pen = new Pen(HourBrush, HourThickness);

        drawingContext.DrawEllipse(HourBrush, pen, center, HourThickness * 1, HourThickness * 1);
        var points = LineOnCircle(
            Math.PI * 2.0 * (Time.Hour / 12.0 + Time.Minute / 60.0 / 12.0) - Math.PI / 2.0, 
            center, 
            0, radius * HourIndicatorRatio);
        drawingContext.DrawLine(pen, points[0], points[1]);
    }


    private void RenderBorder(DrawingContext drawingContext, double radius, Point center)
    {
        drawingContext.DrawEllipse(Background, new Pen(BorderBrush, BorderThickness.Left), center, radius, radius);
    }

    private void RenderHourTicks(DrawingContext drawingContext, double radius, Point center)
    {
        var pen = new Pen(HourTickBrush, HourTickThickness);
        for (var i = 0; i < 12; i++)
        {
            var points = LineOnCircle(Math.PI * 2 * i / 12, center, radius * (1 - HourTickRatio), radius - BorderThickness.Left * 0.5);
            drawingContext.DrawLine(pen, points[0], points[1]);
        }
    }

    private void RenderMinuteTicks(DrawingContext drawingContext, double radius, Point center)
    {
        var pen = new Pen(MinuteTickBrush, MinuteTickThickness);
        for (var i = 0; i < 60; i++)
        {
            if (i % 5 == 0)
            {
                continue; // Skip places where we already have an hour tick
            }

            var points = LineOnCircle(Math.PI * 2 * i / 60.0, center, radius * (1 - MinuteTickRatio), radius - BorderThickness.Left * 0.5);
            drawingContext.DrawLine(pen, points[0], points[1]);
        }
    }
}
