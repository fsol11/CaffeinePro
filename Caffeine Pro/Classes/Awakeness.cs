using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Caffeine_Pro.Services;

namespace Caffeine_Pro.Classes;

/// <summary>
/// Represents the state of awakeness.
/// </summary>
public class Awakeness : INotifyPropertyChanged, IEquatable<Awakeness>
{
    public static readonly Awakeness Default = new();

    public static readonly Awakeness Indefinite = new()
    {
        UntilDateTime = DateTime.MaxValue
    };

    public enum AwakenessTypes
    {
        Absolute,
        Relative
    }

    private DateTime _untilDateTime = DateTime.MaxValue;
    private SessionAction _afterwardsAction;
    private AwakenessOptions _options = new();
    private string _statusText = string.Empty;
    private string _untilDateTimeText = string.Empty;
    private bool _isIndefinite;
    private TimeSpan _relativeSpan;
    private AwakenessTypes _awakenessType = AwakenessTypes.Absolute;
    private bool _isRelative;

    /// <summary>
    /// Initializes a new instance of the <see cref="Awakeness"/> class.
    /// </summary>
    public Awakeness()
    {
        AwakenessType = AwakenessTypes.Absolute;
        SetStatusText();
    }

    public Awakeness(DateTime untilDateTime)
    {
        AwakenessType = AwakenessTypes.Absolute;
        UntilDateTime = untilDateTime;
    }

    public Awakeness(TimeSpan relativeSpan)
    {
        AwakenessType = AwakenessTypes.Relative;
        RelativeSpan = relativeSpan;
    }

    /// <summary>
    /// Gets a value indicating whether the awakeness is indefinite.
    /// </summary>
    [JsonIgnore]
    public bool IsIndefinite
    {
        get => _isIndefinite;
        private set => SetField(ref _isIndefinite, value);
    }

    [JsonIgnore]
    public bool IsRelative
    {
        get => _isRelative;
        private set => SetField(ref _isRelative, value);
    }


    public AwakenessTypes AwakenessType
    {
        get => _awakenessType;
        private set
        {
            SetField(ref _awakenessType, value);
            IsRelative = (value == AwakenessTypes.Relative);
        }
    }

    public TimeSpan RelativeSpan
    {
        get => _relativeSpan;
        private set
        {
            SetField(ref _relativeSpan, value);
            if(IsRelative)
            {
                UntilDateTime = Routines.GetDateTimeFromTimeSpan(value);
            }

            SetStatusText();
        }
    }

    /// <summary>
    /// Gets or sets the date and time until which the awakeness lasts.
    /// </summary>
    [JsonIgnore]
    public DateTime UntilDateTime
    {
        get => _untilDateTime;
        private set
        {
            if (value == DateTime.MaxValue && AwakenessType != AwakenessTypes.Absolute)
            {
                AwakenessType = AwakenessTypes.Absolute;
                RelativeSpan = TimeSpan.Zero;
            }
            else if (AwakenessType == AwakenessTypes.Absolute)
            {
                RelativeSpan = value - DateTime.Now;
            }

            SetField(ref _untilDateTime, value);
            SetStatusText();
        }
    }

    /// <summary>
    /// Gets the text representation of the date and time until which the awakeness lasts.
    /// </summary>
    [JsonIgnore]
    public string UntilDateTimeText
    {
        get => _untilDateTimeText;
        private set => SetField(ref _untilDateTimeText, value);
    }

    /// <summary>
    /// Gets or sets the action to be performed after the awakeness.
    /// </summary>
    public SessionAction AfterwardsAction
    {
        get => _afterwardsAction;
        set
        {
            SetField(ref _afterwardsAction, value);
            SetStatusText();
        }
    }

    /// <summary>
    /// Gets or sets the options for the awakeness.
    /// </summary>
    public AwakenessOptions Options
    {
        get => _options;
        set
        {
            SetField(ref _options, value);
            SetStatusText();
            _options.PropertyChanged += (_, _) => SetStatusText();
        }
    }

    /// <summary>
    /// Gets the status text of the awakeness.
    /// </summary>
    [JsonIgnore]
    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    /// <summary>
    /// Increases the duration of the awakeness by 15 minutes.
    /// </summary>
    public void Plus15Minutes()
    {
        var minutes = 15;
        var maxDateTime = DateTime.Now.AddHours(24);

        var newDate = ((UntilDateTime == DateTime.MaxValue || UntilDateTime == DateTime.MinValue)
            ? DateTime.Now
            : UntilDateTime);
        newDate = newDate.AddMinutes(minutes);

        if (newDate > maxDateTime)
        {
            minutes += (maxDateTime - newDate).Minutes;
            newDate = maxDateTime;
        }

        RelativeSpan = RelativeSpan.Add(TimeSpan.FromMinutes(minutes));
        UntilDateTime = newDate;
    }

    /// <summary>
    /// Decreases the duration of the awakeness by 15 minutes.
    /// </summary>
    public void Minus15Minutes()
    {
        var minutes = -15;
        var minDateTime = DateTime.Now.AddMinutes(15);

        var newDate = ((UntilDateTime == DateTime.MaxValue || UntilDateTime == DateTime.MinValue)
            ? DateTime.Now
            : UntilDateTime);
        newDate = newDate.AddMinutes(minutes);

        if (newDate < minDateTime)
        {
            minutes += (minDateTime - newDate).Minutes;
            newDate = minDateTime;
        }

        RelativeSpan = RelativeSpan.Add(TimeSpan.FromMinutes(minutes));
        UntilDateTime = newDate;
    }

    private void SetStatusText()
    {
        IsIndefinite = UntilDateTime == DateTime.MaxValue;
        IsRelative = AwakenessType == AwakenessTypes.Relative;
        UntilDateTimeText = Routines.GetDateTimeString(UntilDateTime);

        var t = AwakenessType switch
        {
            AwakenessTypes.Relative => $"for {RelativeSpan:hh\\:mm}h",
            AwakenessTypes.Absolute => $"until {UntilDateTimeText}",
            _ => string.Empty
        };

        StatusText = $"{t} - afterwards {Routines.GetEnumDescription(AfterwardsAction)}";
    }

    //------------------------------------------------------------------------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //------------------------------------------------------------------------------------------
    public bool Equals(Awakeness? other)
    {
        return (other != null &&
                _untilDateTime == other._untilDateTime &&
                _afterwardsAction == other._afterwardsAction &&
                _options.Equals(other._options));
    }

    public override bool Equals(object? obj) => Equals(obj as Awakeness);

    public override int GetHashCode() => throw new NotImplementedException();

    // Implicit operator to convert Awakeness to AnalogTime
    public static implicit operator AnalogTime(Awakeness awakeness)
        => new(awakeness.UntilDateTime.TimeOfDay);

    // Implicit operator to convert AnalogTime to Awakeness
    public static implicit operator Awakeness(AnalogTime analogTime)
        => new(Routines.GetDateTimeFromTimeSpan(analogTime.ToTimeSpan()));
}