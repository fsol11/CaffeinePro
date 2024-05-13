using System.Windows;
using System.Windows.Input;
using Caffeine_Pro.Controls;
using static Caffeine_Pro.Controls.ClockControl;

namespace Caffeine_Pro.Classes;

/// <summary>
/// Controls mouse and touch input for the TimePicker control https://github.com/roy-t/TimePicker    
/// </summary>
public class ClockControlInputController : IDisposable
{
    private readonly ClockControl _clockControl;

    // TimePicker.ActualHeight * MinDistanceRatio is the max
    // distance away from the tip of the indicator you can 
    // click to still start dragging it
    private const double MinDistanceRatio = 0.2;

    private Indicator _indicator;
    private bool _isDragging;
    private int _previousMinutes;
    private int _previousHours;

    public ClockControlInputController(ClockControl clockControl)
    {
        _clockControl = clockControl;

        _clockControl.PreviewMouseLeftButtonDown += ClockControlMouseLeftButtonDown;
        _clockControl.PreviewMouseMove += ClockControlMouseMove;
        _clockControl.MouseLeave += ClockControlMouseLeave;
        _clockControl.PreviewMouseLeftButtonUp += ClockControlMouseLeftButtonUp;
    }
    ~ClockControlInputController()
    {
        ReleaseUnmanagedResources();
    }

    private void StartDragging(Point mouse)
    {
        // TODO: highlight indicator that you're dragging
        FindIndicator(mouse);

        _isDragging = true;
    }

    private void StopDragging()
    {
        _indicator = Indicator.None;
        _isDragging = false;
        _previousMinutes = -1;
        _previousHours = -1;
    }

    private void MoveHands(MouseEventArgs e)
    {
        var width = _clockControl.ActualWidth;
        var height = _clockControl.ActualHeight;
        var center = new Point(width / 2.0, height / 2.0);
        var mouse = e.GetPosition(_clockControl);
        var hds = _clockControl.Time.HalfDaySign;
        var time = _clockControl.Time;
        var hours = time.Hour;
        var minutes = time.Minute;

        switch (_indicator)
        {
            case Indicator.HourIndicator:
                {
                    hours = ClockMath.AngleToHour(center, mouse);
                    break;
                }
            case Indicator.MinuteIndicator:
                {
                    minutes = ClockMath.AngleToMinutes(center, mouse);
                    var hourAdjust = _previousMinutes switch
                    {
                        >= 50 and <= 60 when minutes is >= 0 and <= 10 => 1,
                        >= 0 and <= 10 when minutes is >= 50 and <= 60 => -1,
                        _ => 0
                    };
                    hours = time.Hour + hourAdjust;
                    break;
                }
        }

        if ((_previousHours == 11 && hours == 0) || (_previousHours == 0 && hours == 11))
        {
            hds = hds == HalfDaySign.AM ? HalfDaySign.PM : HalfDaySign.AM;
        }
        _clockControl.Time = new AnalogTime(hours, minutes, hds);
        _previousMinutes = minutes;
        _previousHours = hours;
    }

    private void ClockControlMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        MoveHands(e);
    }

    private void FindIndicator(Point mouse)
    {
        var width = _clockControl.ActualWidth - _clockControl.Padding.Left - _clockControl.Padding.Right;
        var height = _clockControl.ActualHeight - _clockControl.Padding.Top - _clockControl.Padding.Bottom;

        var radius = (Math.Min(width, height) - _clockControl.BorderThickness.Left) / 2.0;
        var center = new Point(_clockControl.Padding.Left + width / 2.0, _clockControl.Padding.Top + height / 2.0);
        
        var minuteTip = ClockMath.LineOnCircle((Math.PI * 2 * _clockControl.Time.Minute / 60) - Math.PI / 2.0, center, 0, radius * MinuteIndicatorRatio)[1];
        var hourTip = ClockMath.LineOnCircle((Math.PI * 2 * _clockControl.Time.Hour / 12) - Math.PI / 2.0, center, 0, radius * HourIndicatorRatio)[1];

        var maxDistance = width * MinDistanceRatio;

        var minuteDistance = ClockMath.Distance(mouse, minuteTip);
        var hourDistance = ClockMath.Distance(mouse, hourTip);

        var hourIndicator = (hourDistance <= minuteDistance && hourDistance < maxDistance);
        var minuteIndicator = (minuteDistance < hourDistance && minuteDistance < maxDistance);

        if (minuteIndicator || !hourIndicator)
        {
            _indicator = Indicator.MinuteIndicator;
        }
        else if (hourDistance <= minuteDistance && hourDistance < maxDistance)
        {
            _indicator = Indicator.HourIndicator;
        }
        
    }

    private void ClockControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        StartDragging(e.GetPosition(_clockControl));
        MoveHands(e);
    }

    private void ClockControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        StopDragging();
    }

    private void ClockControlMouseLeave(object sender, MouseEventArgs e)
    {
        StopDragging();
    }

    private enum Indicator
    {
        None,
        HourIndicator,
        MinuteIndicator
    }

    private void ReleaseUnmanagedResources()
    {
        _clockControl.PreviewMouseLeftButtonDown -= ClockControlMouseLeftButtonDown;
        _clockControl.PreviewMouseMove -= ClockControlMouseMove;
        _clockControl.MouseLeave -= ClockControlMouseLeave;
        _clockControl.PreviewMouseLeftButtonUp -= ClockControlMouseLeftButtonUp;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}
