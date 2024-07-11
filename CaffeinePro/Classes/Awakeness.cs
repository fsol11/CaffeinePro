using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using CaffeinePro.Services;

namespace CaffeinePro.Classes;

/// <summary>
/// Represents the state of awakeness.
/// </summary>
public sealed class Awakeness : IEquatable<Awakeness>, INotifyPropertyChanged
{
    public static readonly Awakeness Indefinite = new();
    private string _endDateTimeText = string.Empty;
    private string _endDateText = string.Empty;
    private string _endTimeText = string.Empty;

    public enum AwakenessTypes
    {
        Absolute,
        Relative
    }

    [JsonConstructor]
    public Awakeness(AwakenessTypes awakenessType, TimeSpan relativeSpan, AwakenessOptions options,
        SessionAction afterwardsAction)
    {
        IsRelative = awakenessType == AwakenessTypes.Relative;
        RelativeSpan = relativeSpan;
        AwakenessType = awakenessType;
        Options = options;
        AfterwardsAction = afterwardsAction;

        if (relativeSpan == TimeSpan.Zero || relativeSpan == TimeSpan.MaxValue)
        {
            EndDateTime = DateTime.MaxValue;
            AwakenessType = AwakenessTypes.Absolute;
            IsIndefinite = true;
        }
        else
        {
            EndDateTime = Routines.GetDateTimeFromTimeSpan(relativeSpan, awakenessType);
        }

        UpdateTexts();
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Awakeness"/> class.
    /// </summary>
    public Awakeness() : this(AwakenessTypes.Absolute, TimeSpan.Zero, new(), new()) // Indefinite
    {
    }

    public Awakeness(DateTime untilDateTime, AwakenessOptions options, SessionAction afterwardsAction) : this(
        AwakenessTypes.Absolute, untilDateTime.TimeOfDay, options, afterwardsAction)
    {
    }

    public Awakeness(TimeSpan relativeSpan, AwakenessOptions options, SessionAction afterwardsAction) : this(
        AwakenessTypes.Relative, relativeSpan, options, afterwardsAction)
    {
    }

    public Awakeness(Awakeness awakeness) : this(awakeness.AwakenessType, awakeness.RelativeSpan, awakeness.Options,
        awakeness.AfterwardsAction)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the awakeness is indefinite.
    /// </summary>
    public bool IsIndefinite
    {
        get;
    }

    public bool IsRelative
    {
        get;
    }


    public AwakenessTypes AwakenessType
    {
        get;
    }


    public TimeSpan RelativeSpan
    {
        get;
    }

    public DateTime EndDateTime
    {
        get;
    }

    public string EndDateTimeText
    {
        get => _endDateTimeText;
        private set => SetField(ref _endDateTimeText, value);
    }

    public string EndDateText
    {
        get => _endDateText;
        private set => SetField(ref _endDateText, value);
    }

    public string EndTimeText
    {
        get => _endTimeText;
        private set => SetField(ref _endTimeText, value);
    }

    /// <summary>
    /// Gets the action to be performed after the awakeness.
    /// </summary>
    public SessionAction AfterwardsAction
    {
        get;
    }

    /// <summary>
    /// Gets the options for the awakeness.
    /// </summary>
    public AwakenessOptions Options
    {
        get;
    }


    /// <summary>
    /// Decreases the duration of the awakeness by 15 minutes.
    /// </summary>
    public Awakeness AddMinutes(int minutes)
    {
        var newTime = IsIndefinite ? DateTime.Now.TimeOfDay : RelativeSpan;
        newTime = newTime.Add(new TimeSpan(0, minutes, 0));

        return new Awakeness(AwakenessType, newTime, Options, AfterwardsAction);
    }


    //------------------------------------------------------------------------------------------
    public bool Equals(Awakeness? other)
    {
        return (other != null &&
                EndDateTime == other.EndDateTime &&
                AfterwardsAction == other.AfterwardsAction &&
                Options.Equals(other.Options));
    }

    public override bool Equals(object? obj) => Equals(obj as Awakeness);

    public override int GetHashCode() => HashCode.Combine(AwakenessType, RelativeSpan, AfterwardsAction, Options);

    public static bool operator ==(Awakeness? left, Awakeness? right) => Equals(left, right);

    public static bool operator !=(Awakeness? left, Awakeness? right) => !(left == right);

    public static Awakeness RenewDateTime(Awakeness awakeness)
    {
        return new Awakeness(awakeness);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    public void UpdateTexts()
    {
        EndDateText = Routines.GetDateString(EndDateTime);
        EndTimeText = IsIndefinite ? string.Empty : EndDateTime.ToString("hh:mm tt");
        EndDateTimeText = Routines.GetDateTimeString(EndDateTime);
    }

    public string GetAwakenessDescription()
    {
        var s = $"Until {EndDateTimeText}";
        if (AfterwardsAction != SessionAction.None)
        {
            s += $" - afterwards {Routines.GetEnumDescription(AfterwardsAction)}";
        }

        return s;
    }
}