using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Caffeine_Pro.Classes;

/// <summary>
/// Settings class for the application
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private bool _startInactive;
    private bool _allowScreenSaver;
    private bool _noIcon;
    private bool _deactivateWhenLocked;
    private bool _deactivateOnBattery;
    private bool _deactivateWhenCpuBelowPercentage;
    private bool _startWithWindows;
    private int _cpuUsage = 5;

    /// <summary>
    /// Get/set the value of the start with windows setting
    /// When true, the application will start with windows
    /// </summary>
    public bool StartWithWindows
    {
        get => Routines.IsAddedToWindowsStartup();
        set
        {
            Routines.AddToWindowsStartup(value);
            SetField(ref _startWithWindows, value);
        }
    }

    /// <summary>
    /// Get/set the value of the start inactive setting
    /// When true, the application will start inactive
    /// </summary>
    public bool StartInactive
    {
        get => _startInactive;
        set => SetField(ref _startInactive, value);
    }

    /// <summary>
    /// Get/set the value of the allow screen saver setting
    /// When true, the application will allow the screen saver to start
    /// however, keystrokes will not be simulated. Therefore, applications
    /// that track inactivity, such as Skype, will detect inactivity.
    /// </summary>
    public bool AllowScreenSaver
    {
        get => _allowScreenSaver;
        set => SetField(ref _allowScreenSaver, value);
    }

    /// <summary>
    /// Get/set the value of the no icon setting
    /// When true, the application will not show an icon in the system tray.
    /// And the only way to interact with the application is through the command line.
    /// </summary>
    public bool NoIcon
    {
        get => _noIcon;
        set => SetField(ref _noIcon, value);
    }

    /// <summary>
    /// Get/set the value of deactivate when locked setting
    /// When true, the application will deactivate when the computer is locked
    /// When application is deactivated, it will have to be reactivated manually.
    /// </summary>
    public bool DeactivateWhenLocked
    {
        get => _deactivateWhenLocked;
        set => SetField(ref _deactivateWhenLocked, value);
    }

    /// <summary>
    /// Get/set the value of deactivate on battery setting
    /// When true, the application will deactivate when the computer is on battery.
    /// When application is deactivated, it will have to be reactivated manually.
    /// </summary>
    public bool DeactivateOnBattery
    {
        get => _deactivateOnBattery;
        set => SetField(ref _deactivateOnBattery, value);
    }

    /// <summary>
    /// Get/set the value of deactivate when CPU below percentage setting.
    /// When true, the application will deactivate when the CPU usage is below the value defined in <see cref="CpuUsage"/> property
    /// </summary>
    public bool DeactivateWhenCpuBelowPercentage
    {
        get => _deactivateWhenCpuBelowPercentage;
        set => SetField(ref _deactivateWhenCpuBelowPercentage, value);
    }

    /// <summary>
    /// Get/set the value of the CPU usage percentage.
    /// When <see cref="DeactivateWhenCpuBelowPercentage"/> is True, this value determines
    /// the CPU usage percentage below which the application will deactivate.
    /// </summary>
    public int CpuUsage
    {
        get => _cpuUsage;
        set => SetField(ref _cpuUsage, value);
    }

    /// <summary>
    /// Reset the settings to their default values
    /// </summary>
    public void Reset()
    {
        var reset = new AppSettings();
        StartInactive = reset.StartInactive;
        AllowScreenSaver = reset.AllowScreenSaver;
        NoIcon = reset.NoIcon;
        DeactivateWhenLocked = reset.DeactivateWhenLocked;
        DeactivateOnBattery = reset.DeactivateOnBattery;
        DeactivateWhenCpuBelowPercentage = reset.DeactivateWhenCpuBelowPercentage;
        CpuUsage = reset.CpuUsage;
    }

    // INotifyPropertyChanged implementation -----------------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

}
