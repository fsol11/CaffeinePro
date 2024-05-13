namespace TimePicker;

/// <summary>
///  Represents a time, rules for AM and PM from 
///  http://www.timeanddate.com/time/am-and-pm.html
/// </summary>
public readonly struct AnalogTime : IComparable<AnalogTime>, IEquatable<AnalogTime>, IFormattable
{
    public AnalogTime(int hour = 0, int minute = 0, HalfDaySign halfDaySign = HalfDaySign.AM)
    {
        HalfDaySign = halfDaySign;

        // Adjusting the time to be within the valid range
        while (minute >= 60)
        {
            minute -= 60;
            hour++;
        }

        while (minute < 0)
        {
            minute += 60;
            hour--;
        }

        while (hour >= 12)
        {
            if (hour == 12 && halfDaySign == HalfDaySign.AM)
            {
                hour = 0;
            }
            else
            {
                hour -= 12;
                HalfDaySign = ToggleHalfDaySign(HalfDaySign);
            }
        }

        while (hour < 0)
        {
            hour += 12;
            HalfDaySign = ToggleHalfDaySign(HalfDaySign);
        }

        Hour = hour;
        Minute = minute;
    }


    private static HalfDaySign ToggleHalfDaySign(HalfDaySign hds) => hds == HalfDaySign.AM ? HalfDaySign.PM : HalfDaySign.AM;

    public AnalogTime(double minutes) : this((int)minutes / 60, (int)minutes % 60)
    {

    }

    public AnalogTime(TimeSpan time)
    {
        Minute = time.Minutes;

        switch (time.Hours)
        {
            case 0:
                // 12:00 AM to 12:59 AM
                Hour = time.Hours + 12;
                HalfDaySign = HalfDaySign.AM;
                break;
            case <= 11:
                // 01:00 AM to 11:59 AM
                Hour = time.Hours;
                HalfDaySign = HalfDaySign.AM;
                break;
            case 12:
                // 12:00PM to 12:59 PM
                Hour = time.Hours;
                HalfDaySign = HalfDaySign.PM;
                break;
            case < 24:
                // 01:00PM to 11:59 PM
                Hour = time.Hours - 12;
                HalfDaySign = HalfDaySign.PM;
                break;
            default:
                throw new ArgumentException("Cannot create a time from a TimeSpan that represents more than 24 hours", nameof(time));
        }
    }

    public AnalogTime(DigitalTime time)
        : this(time.ToTimeSpan()) { }

    public int Hour
    {
        get;
    }
    public int Minute
    {
        get;
    }

    public HalfDaySign HalfDaySign
    {
        get;
    }

    public static AnalogTime Default
    {
        get;
    } = AnalogTime.FromMinutes(0);

    public TimeSpan ToTimeSpan()
    {
        if (HalfDaySign == HalfDaySign.AM)
        {
            return Hour == 12 ?
                // 12:00 AM to 12:59 AM is 00:00 to 00:59
                new TimeSpan(Hour - 12, Minute, 0) :
                // 01:00 AM to 11:59 AM is 01:00 to 11:59
                new TimeSpan(Hour, Minute, 0);
        }
        else
        {
            return Hour == 12 ?
                // 12:00 PM to 12:59 PM is 12:00 to 12:59
                new TimeSpan(Hour, Minute, 0) :
                // 01:00 PM to 11:59 PM is 13:00 to 23:59 
                new TimeSpan(Hour + 12, Minute, 0);
        }
    }

    public int TotalMinutes => Hour * 60 + Minute + ((HalfDaySign == HalfDaySign.PM) ? 12 * 60 : 0);

    public DateTime ToDateTime()
    {
        var time = ToTimeSpan();
        var day = DateTime.Today;
        if (time < DateTime.Now.TimeOfDay)
        {
            day = day.AddDays(1);
        }

        return day.Add(time);
    }

    public DigitalTime ToDigitalTime()
    {
        return new DigitalTime(ToTimeSpan());
    }

    public override int GetHashCode()
    {
        return ((int)HalfDaySign * 1000) + Hour * 100 + Minute;
    }

    public override bool Equals(object? obj)
    {
        return obj is AnalogTime time && Equals(time);
    }

    public bool Equals(AnalogTime other)
    {
        return other.Hour == Hour &&
               other.Minute == Minute &&
               other.HalfDaySign == HalfDaySign;
    }

    public override string ToString()
    {
        var dt = DateTime.Today.Add(ToTimeSpan());
        return dt.ToString("HH:mm tt");
    }

    public string ToString(string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return string.Empty;
        }

        var hasD = format.Contains("D");
        if (!hasD)
        {
            return ToString(format);
        }

        var i = format.IndexOf("D", StringComparison.Ordinal);
        var before = hasD ? format[..i] : string.Empty;
        var after = hasD ? format[(i + 1)..] : string.Empty;

        var day = DateTime.Today;
        var time = ToTimeSpan();

        if (hasD && DateTime.Now.TimeOfDay > time)
        {
            day = day.AddDays(1);
        }

        var dayString = (day - DateTime.Today).Days switch
        {
            -3 => "3 days ago",
            -2 => "2 days ago",
            -1 => "Yesterday",
            1 => "Tomorrow",
            2 => "In 2 days",
            3 => "In 3 days",
            _ => string.Empty
        };

        return ToString(before) + dayString + ToString(after);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString(format);
    }

    public int CompareTo(AnalogTime other)
    {
        return ToTimeSpan().CompareTo(other.ToTimeSpan());
    }

    public static bool operator <(AnalogTime left, AnalogTime right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(AnalogTime left, AnalogTime right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator ==(AnalogTime left, AnalogTime right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(AnalogTime left, AnalogTime right)
    {
        return !Equals(left, right);
    }

    public static implicit operator TimeSpan(AnalogTime analogTime)
    {
        return analogTime.ToTimeSpan();
    }

    public static implicit operator AnalogTime(TimeSpan timeSpan)
    {
        return new AnalogTime(timeSpan);
    }

    public static implicit operator double(AnalogTime analogTime)
    {
        return analogTime.Hour * 60 + analogTime.Minute;
    }

    public static AnalogTime FromMinutes(int minutes)
    {
        return new AnalogTime(0, minutes);
    }
}
