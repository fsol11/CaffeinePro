using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Caffeine_Pro.Classes;
             
public class AwakenessSettings : INotifyPropertyChanged, IEquatable<AwakenessSettings>
{
    public bool Activate
    {
        get => _activate;
        set => SetField(ref _activate, value);
    }
    public DateTime UntilDateTime
    {
        get => _untilDateTime;
        set => SetField(ref _untilDateTime, value);
    }
    
    public WindowsSessionControl.SessionAction AfterwardsAction
    {
        get => _afterwardsAction;
        set => SetField(ref _afterwardsAction, value);
    }
    
    public AwakenessOptions? Options
    {
        get => _options;
        set => SetField(ref _options, value);
    }
    
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
    
    private DateTime _untilDateTime = DateTime.MinValue;
    private WindowsSessionControl.SessionAction _afterwardsAction;
    private AwakenessOptions? _options;
    private bool _activate;
    
    public bool Equals(AwakenessSettings? other) {
        return (other != null && _activate == other._activate && _untilDateTime == other._untilDateTime &&
                _afterwardsAction == other._afterwardsAction && _options == other._options);
    }
}