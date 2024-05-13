using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Caffeine_Pro.Classes;

public sealed class AwakenessOptions : INotifyPropertyChanged, IEquatable<AwakenessOptions>
{
    private int _cpuBelowPercentage = 5;
    private bool _deactivateWhenCpuBelowPercentage;
    private bool _deactivateWhenLocked;
    private bool _allowScreenSaver;
    private bool _deactivateWhenOnBattery;

    public bool AllowScreenSaver
    {
        get => _allowScreenSaver;
        set => SetField(ref _allowScreenSaver, value);
    }

    public bool DeactivateWhenLocked
    {
        get => _deactivateWhenLocked;
        set => SetField(ref _deactivateWhenLocked, value);
    }

    public bool DeactivateWhenOnBattery
    {
        get => _deactivateWhenOnBattery;
        set => SetField(ref _deactivateWhenOnBattery, value);
    }

    public bool DeactivateWhenCpuBelowPercentage
    {
        get => _deactivateWhenCpuBelowPercentage;
        set => SetField(ref _deactivateWhenCpuBelowPercentage, value);
    }

    public int CpuBelowPercentage
    {
        get => _cpuBelowPercentage;
        set => SetField(ref _cpuBelowPercentage, value);
    }
    
    public bool AnyOptionsSet => AllowScreenSaver || DeactivateWhenLocked || DeactivateWhenOnBattery || DeactivateWhenCpuBelowPercentage; 
    
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
        OnPropertyChanged(nameof(AnyOptionsSet));
    }

    public bool Equals(AwakenessOptions? other)
    {
        if(other is null)
        {
            return false;
        }

        return (other._allowScreenSaver == _allowScreenSaver &&
                other._deactivateWhenLocked == _deactivateWhenLocked &&
                other._deactivateWhenOnBattery == _deactivateWhenOnBattery &&
                other._deactivateWhenCpuBelowPercentage == _deactivateWhenCpuBelowPercentage &&
                other._cpuBelowPercentage == _cpuBelowPercentage);
    }
    
    public void Reset()
    {
        CpuBelowPercentage = 5;
        DeactivateWhenCpuBelowPercentage = false;
        DeactivateWhenLocked = false;
        AllowScreenSaver = false;
        DeactivateWhenOnBattery = false;
    }
}