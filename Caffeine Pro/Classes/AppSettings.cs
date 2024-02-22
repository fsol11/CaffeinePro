using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Caffeine_Pro.Classes;

internal class AppSettings : INotifyPropertyChanged
{
    private bool _startInactive;
    private bool _allowScreenSaver;
    private bool _noIcon;
    private bool _deactivateWhenLocked;
    private bool _deactivateOnBattery;
    private bool _deactivateWhenCpuBelowPercentage;
    private bool _startWithWindows;
    private int _cpuUsage;
    private string _culture = "en";
    public static AppSettings Default { get; } = new();

    public AppSettings()
    {
        Load();
    }


    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            Settings.Default.StartWithWindows = value;
            Routines.AddToWindowsStartup(value);
            SetField(ref _startWithWindows, value);
        }
    }

    public bool StartInactive
    {
        get => _startInactive;
        set
        {
            Settings.Default.StartInactive = value;
            SetField(ref _startInactive, value);
        }
    }

    public bool AllowScreenSaver
    {
        get => _allowScreenSaver;
        set
        {
            Settings.Default.AllowScreenSaver = value;
            SetField(ref _allowScreenSaver, value);
        }
    }

    public bool NoIcon
    {
        get => _noIcon;
        set => SetField(ref _noIcon, value);
    }

    public bool DeactivateWhenLocked
    {
        get => _deactivateWhenLocked;
        set
        {
            Settings.Default.DeactivateWhenLocked = value;
            SetField(ref _deactivateWhenLocked, value);
        }
    }

    public bool DeactivateOnBattery
    {
        get => _deactivateOnBattery;
        set
        {
            Settings.Default.DeactivateOnBattery = value;
            SetField(ref _deactivateOnBattery, value);
        }
    }

    public bool DeactivateWhenCpuBelowPercentage
    {
        get => _deactivateWhenCpuBelowPercentage;
        set
        {
            Settings.Default.DeactivateWhenCpuBelowPercentage = value;
            SetField(ref _deactivateWhenCpuBelowPercentage, value);
        }
    }

    public int CpuUsage
    {
        get => _cpuUsage;
        set
        {
            Settings.Default.CpuUsage = value;
            SetField(ref _cpuUsage, value);
        }
    }

    public string Culture
    {
        get => _culture;
        set => SetField(ref _culture, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Settings.Default.Save();
        OnPropertyChanged(propertyName);
        return true;
    }

    public void Reset()
    {
        var reset = new AppSettings();
        StartWithWindows = reset.StartWithWindows;
        StartInactive = reset.StartInactive;
        AllowScreenSaver = reset.AllowScreenSaver;
        NoIcon = reset.NoIcon;
        DeactivateWhenLocked = reset.DeactivateWhenLocked;
        DeactivateOnBattery = reset.DeactivateOnBattery;
        DeactivateWhenCpuBelowPercentage = reset.DeactivateWhenCpuBelowPercentage;
        CpuUsage = reset.CpuUsage;
        Culture = reset.Culture;
    }

    public void Load()
    {
        StartWithWindows = Settings.Default.StartWithWindows;
        StartInactive = Settings.Default.StartInactive;
        AllowScreenSaver = Settings.Default.AllowScreenSaver;
        DeactivateWhenLocked = Settings.Default.DeactivateWhenLocked;
        DeactivateOnBattery = Settings.Default.DeactivateOnBattery;
        DeactivateWhenCpuBelowPercentage = Settings.Default.DeactivateWhenCpuBelowPercentage;
        CpuUsage = Settings.Default.CpuUsage;
        Culture = Settings.Default.Culture;
    }

    public void Save()
    {
        Settings.Default.StartWithWindows = StartWithWindows;
        Settings.Default.StartInactive = StartInactive;
        Settings.Default.AllowScreenSaver = AllowScreenSaver;
        Settings.Default.DeactivateWhenLocked = DeactivateWhenLocked;
        Settings.Default.DeactivateOnBattery = DeactivateOnBattery;
        Settings.Default.DeactivateWhenCpuBelowPercentage = DeactivateWhenCpuBelowPercentage;
        Settings.Default.CpuUsage = CpuUsage;
        Settings.Default.Culture = Culture;
        Settings.Default.Save();
    }
}
