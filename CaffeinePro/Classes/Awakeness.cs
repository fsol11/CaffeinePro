using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
    public Awakeness(AwakenessTypes awakenessType, TimeSpan relativeSpan)
    {
        IsRelative = awakenessType == AwakenessTypes.Relative;
        RelativeSpan = relativeSpan;
        AwakenessType = awakenessType;

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
    public Awakeness() : this(AwakenessTypes.Absolute, TimeSpan.Zero) // Indefinite
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

    public static TimeSpan GetTimeOfDay() => new(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0);
    public static TimeOnly GetTimeOnly() => new(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0);
    public static DateTime GetNow() => new(DateOnly.FromDateTime(DateTime.Now), GetTimeOnly());
    /// <summary>
    /// Decreases the duration of the awakeness by 15 minutes.
    /// </summary>
    public Awakeness AddMinutes(int minutes)
    {
        var newTime = IsIndefinite ? GetTimeOfDay() : RelativeSpan;
        newTime = newTime.Add(new TimeSpan(0, minutes, 0));

        return new Awakeness(AwakenessType, newTime);
    }


    //------------------------------------------------------------------------------------------
    public bool Equals(Awakeness? other)
    {
        return (other != null &&
                EndDateTime == other.EndDateTime);
    }

    //------------------------------------------------------------------------------------------
    public bool EqualsExceptDate(Awakeness? other)
    {
        return (other != null &&
                EndDateTime.TimeOfDay == other.EndDateTime.TimeOfDay);
    }

    public override bool Equals(object? obj) => Equals(obj as Awakeness);

    public override int GetHashCode() => HashCode.Combine(AwakenessType, RelativeSpan);

    public static bool operator ==(Awakeness? left, Awakeness? right) => Equals(left, right);

    public static bool operator !=(Awakeness? left, Awakeness? right) => !(left == right);

    public static Awakeness RenewDateTime(Awakeness awakeness)
    {
        return new Awakeness(awakeness.AwakenessType, awakeness.RelativeSpan);
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

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public string GetAwakenessDescription()
    {
        var s = $"Until {EndDateTimeText}";
        var afterwardsAction = App.CurrentApp.AppSettings.AfterwardsAction;
        if (afterwardsAction != SessionAction.None)
        {
            s += $" - afterwards {Routines.GetEnumDescription(afterwardsAction)}";
        }

        return s;
    }

    public Awakeness(string json)
    {
        var tempAwakeness = JsonSerializer.Deserialize<Awakeness>(json);
        if (tempAwakeness == null)
        {
            throw new ArgumentException("Invalid JSON for Awakeness.");
        }

        // Assuming Routines and other necessary methods are static and can be accessed here.
        // Manually copying properties from the deserialized object to this instance.
        // This approach is necessary because many properties are read-only and can't be set directly outside of the constructor.
        IsRelative = tempAwakeness.IsRelative;
        AwakenessType = tempAwakeness.AwakenessType;
        RelativeSpan = tempAwakeness.RelativeSpan;

        // Directly setting fields that back read-only properties since constructors can't access other constructors' parameters directly.
        _endDateTimeText = tempAwakeness.EndDateTimeText;
        _endDateText = tempAwakeness.EndDateText;
        _endTimeText = tempAwakeness.EndTimeText;

        // Handling properties that depend on other properties or need special initialization.
        if (tempAwakeness.IsIndefinite)
        {
            EndDateTime = DateTime.MaxValue;
            IsIndefinite = true;
        }
        else
        {
            EndDateTime = Routines.GetDateTimeFromTimeSpan(RelativeSpan, AwakenessType);
        }

        UpdateTexts(); // Ensure all text representations are updated.
    }
}