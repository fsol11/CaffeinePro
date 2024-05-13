namespace TimePicker;

/// <summary>
/// Represents time as on a digital 24-hour clock
/// </summary>
public readonly struct DigitalTime : IComparable<DigitalTime>, IEquatable<DigitalTime>
{
    public DigitalTime(int hour, int minute)
    {
        if (hour is < 0 or > 23)
        {
            throw new ArgumentException("Hour should be in [0..23]", nameof(hour));
        }

        if (minute is < 0 or > 59)
        {
            throw new ArgumentException("Minute should be in [0..59]", nameof(minute));
        }

        Hour = hour;
        Minute = minute;
    }

    public DigitalTime(TimeSpan time)
        : this(time.Hours, time.Minutes) { }


    public DigitalTime(AnalogTime time)
        : this(time.ToTimeSpan()) { }

    public int Hour
    {
        get;
    }
    public int Minute
    {
        get;
    }

    public TimeSpan ToTimeSpan()
    {
        return new TimeSpan(Hour, Minute, 0);
    }

    public AnalogTime ToAnalogueTime()
    {
        return new AnalogTime(ToTimeSpan());
    }

    public int CompareTo(DigitalTime other)
    {
        return ToTimeSpan().CompareTo(other.ToTimeSpan());
    }

    public override string ToString()
    {
        var dt = DateTime.Today.Add(ToTimeSpan());
        return dt.ToString("HH:mm");
    }

    public override int GetHashCode()
    {
        return (Hour * 100) + Minute;
    }

    public override bool Equals(object? obj)
    {
        return obj is DigitalTime time && Equals(time);
    }

    public bool Equals(DigitalTime other)
    {
        return other.Hour == Hour &&
               other.Minute == Minute;
    }

    public static bool operator <(DigitalTime left, DigitalTime right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(DigitalTime left, DigitalTime right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator ==(DigitalTime left, DigitalTime right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(DigitalTime left, DigitalTime right)
    {
        return !Equals(left, right);
    }
}
