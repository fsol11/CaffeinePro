using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Timers;
using System.Windows;
using CaffeinePro.Classes;
using Notification.Core;
using Notification.Wpf;
using Timer = System.Timers.Timer;

namespace CaffeinePro.Services;

public sealed class KeepAwakeService : INotifyPropertyChanged
{
    private bool _isActive;
    private Awakeness _awakeness;
    private string _statusText = string.Empty;
    private readonly WindowsKeyboardMouseCapture _windowsKeyboardMouseCapture = new();

    ~KeepAwakeService()
    {
        Deactivate();
    }

    public bool IsTemporarilyInactive
    {
        get => _isTemporarilyInactive;
        private set
        {
            if (value == _isTemporarilyInactive)
            {
                return;
            }

            SetField(ref _isTemporarilyInactive, value);
            OnStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsTemporarilyInactiveBecauseSessionLocked
    {
        get => _isTemporarilyInactiveBecauseSessionLocked;
        private set => SetField(ref _isTemporarilyInactiveBecauseSessionLocked, value);
    }

    public bool IsTemporarilyInactiveBecauseOnBattery
    {
        get => _isTemporarilyInactiveBecauseOnBattery;
        private set => SetField(ref _isTemporarilyInactiveBecauseOnBattery, value);
    }

    public bool IsTemporarilyInactiveBecauseCpuBelowPercentage
    {
        get => _isTemporarilyInactiveBecauseCpuBelowPercentage;
        private set => SetField(ref _isTemporarilyInactiveBecauseCpuBelowPercentage, value);
    }

    public Awakeness Awakeness
    {
        get => _awakeness;
        set
        {
            if (value != _awakeness)
            {
                SetField(ref _awakeness, value);
            }

            UpdateStatusText();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public KeepAwakeService(WindowsSessionService windowsSessionService,
        SystemActivityService systemActivityService, NotificationManager notificationManager)
    {
        _windowsSessionService = windowsSessionService;
        _windowsSessionService.OnUnlock += (_, _) => OnUnlock();
        _windowsSessionService.OnLock += (_, _) => OnLock();
        _systemActivityService = systemActivityService;
        _systemActivityService.OnUserBecameActive += (_, _) => OnUserBecameActive();
        _notificationManager = notificationManager;
        _timer.Elapsed += TimerFunction;
        Awakeness = _awakeness = Awakeness.Indefinite;
    }

    private readonly WindowsSessionService _windowsSessionService;
    private readonly SystemActivityService _systemActivityService;
    private readonly NotificationManager _notificationManager;

    /// <summary>
    /// This event is called when keep awake timer is enabled or disabled
    /// </summary>
    public event EventHandler? OnStatusChanged;

    /// <summary>
    /// get/set activation status of the keep awake service
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (value)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }

            SetField(ref _isActive, value);
            _awakeness.UpdateTexts();
            UpdateStatusText();
            OnStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateStatusText()
    {
        StatusText = $"{App.AppName} - {(IsActive ? "Active" : "Inactive")}";
        if (IsActive)
        {
            StatusText += $" - {Awakeness.GetAwakenessDescription()}";
        }
    }



    /// <summary>
    /// Declares the Windows function for setting thread properties
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;

    /// <summary>
    /// Returns a random timer interval between 35 and 120 seconds
    /// </summary>
    /// <returns></returns>
    private static int GetRandomTimerInterval() => RandomNumberGenerator.GetInt32(35000, 119900);

    /// <summary>
    /// The timer that keeps Windows awake
    /// </summary>
    private readonly Timer _timer = new(GetRandomTimerInterval())
    {
        AutoReset = true,
        Enabled = false,
    };

    private bool _isTemporarilyInactive;
    private bool _isTemporarilyInactiveBecauseSessionLocked;
    private bool _isTemporarilyInactiveBecauseOnBattery;
    private bool _isTemporarilyInactiveBecauseCpuBelowPercentage;

    /// <summary>
    /// Activate the keep awake service to the default time
    /// </summary>
    public void ActivateDefault()
    {
        if (App.CurrentApp.AppSettings.StartupAwakeness != null)
        {
            Activate(App.CurrentApp.AppSettings.StartupAwakeness);
        }
    }

    /// <summary>
    /// Activate the keep awake service until a specific date and time
    /// </summary>
    /// <param name="awakeness"></param>
    public void Activate(Awakeness? awakeness = null)
    {
        if (awakeness != null)
        {
            Awakeness = awakeness;
        }

        if (Awakeness.EndDateTime < Awakeness.GetNow())
        {
            Awakeness = Awakeness.RenewDateTime(Awakeness);
        }

        _windowsKeyboardMouseCapture.Hook();

        ResetIgnoreUnlockNotificationDate();
        IsActive = true;
    }

    /// <summary>
    /// Deactivate the keep awake service
    /// </summary>
    public void Deactivate(bool executeAfterwardsAction = false)
    {
        if (_isActive)
        {
            _ = SetThreadExecutionState(EsContinuous); // <- Setting thread state to normal
            IsActive = false;
        }

        _windowsKeyboardMouseCapture.Unhook();

        if (executeAfterwardsAction)
        {
            WindowsSessionService.ExecuteSessionAction(_awakeness.AfterwardsAction);
        }
    }

    /// <summary>
    /// Updates status of the temporarily inactive flags
    /// </summary>
    private void UpdateIsTemporarilyInactive()
    {
        IsTemporarilyInactiveBecauseOnBattery = Awakeness.Options.InactiveWhenOnBattery && Routines.IsOnBattery();
        IsTemporarilyInactiveBecauseCpuBelowPercentage = Awakeness.Options.InactiveWhenCpuBelowPercentage &&
                                                         Routines.CpuUsage() < Awakeness.Options.CpuBelowPercentage;
        IsTemporarilyInactiveBecauseSessionLocked = Routines.IsWorkstationLocked();

        IsTemporarilyInactive = IsTemporarilyInactiveBecauseOnBattery
                                || IsTemporarilyInactiveBecauseCpuBelowPercentage
                                || IsTemporarilyInactiveBecauseSessionLocked;
    }

    /// <summary>
    /// The function that is called at each timer tick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="elapsedEventArgs"></param>
    private void TimerFunction(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        // Handle DeactivateWhenLocked
        Debug.Assert(Awakeness.Options != null);

        UpdateIsTemporarilyInactive();

        if (IsTemporarilyInactive)
        {
            return;
        }

        // Deactivate when the time is up
        if (Awakeness.GetNow() >= Awakeness.EndDateTime)
        {
            Deactivate(true);
            return;
        }


        var timeSinceLastActivity = (DateTime.Now - _windowsKeyboardMouseCapture.LastActivity).TotalMilliseconds;
        var dontSendAwakeSignal = timeSinceLastActivity < _timer.Interval;

        // Setting a new random interval for next tick
        _timer.Interval = GetRandomTimerInterval();

        // If the time since last keyboard/mouse activity is less than the timer interval, wait for the next tick
        if (dontSendAwakeSignal)
        {
            return;
        }

        // Handle AllowScreenSaver
        if (App.CurrentApp.AppSettings.AllowScreenSaver)
        {
            // Prevent Windows from going to sleep, but allow screen saver.
            // NOTE: SetThreadExecutionState does NOT update GetLastInputInfo,
            // so communication apps (Teams, Slack, etc.) will still detect inactivity
            // and show the user as Away when this option is enabled.
            _ = SetThreadExecutionState(EsContinuous | EsSystemRequired);
        }
        else
        {
            // Allow Windows to manage sleep normally (SendInput below keeps it awake indirectly
            // by resetting the idle timer through actual simulated input).
            _ = SetThreadExecutionState(EsContinuous);

            // Simulate input (key press or mouse move) via SendInput.
            // This updates GetLastInputInfo, which is the primary mechanism used by
            // communication apps (Teams, Slack, etc.) to detect user inactivity.
            KeyMouseSimulator.SendKeepAwakeSignal();
        }
    }

    // INotifyPropertyChanged implementation ---------------------------------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnLock()
    {
        IsTemporarilyInactiveBecauseSessionLocked = true;
    }

    private void OnUnlock()
    {
        IsTemporarilyInactiveBecauseSessionLocked = false;
        ConfirmAndSetDefaultAwakeness();
    }

    /// <summary>
    /// Called when the computer becomes active again after the display was turned off
    /// because of inactivity.
    /// </summary>
    private void OnUserBecameActive()
    {
        ConfirmAndSetDefaultAwakeness();
    }

    /// <summary>
    /// Confirms with the user if the program should be activated on startup and sets the default awakeness accordingly.
    /// </summary>
    public void ConfirmAndSetDefaultAwakeness()
    {
        if (IsActive
            || App.CurrentApp.AppSettings.IsUnlockNotificationIgnoredToday
            || App.CurrentApp.AppSettings.StartActive == false
            || App.CurrentApp.AppSettings.StartupAwakeness.EndDateTime.TimeOfDay < Awakeness.GetTimeOfDay())
        {
            return;
        }

        if (App.CurrentApp.AppSettings.StartActive == true)
        {
            ActivateDefault();
            return;
        }

        // Ask user if the program should be activated
        //   When reaching here => App.CurrentApp.AppSettings.StartActive is null
        //   Which means user has selected "Ask Me"
        NotificationWindow.OpenIt(Awakeness.RenewDateTime(App.CurrentApp.AppSettings.StartupAwakeness));
    }

    public void SetIgnoreUnlockNotificationToToday()
    {
        App.CurrentApp.AppSettings.IsUnlockNotificationIgnoredToday = true;
    }

    public void ResetIgnoreUnlockNotificationDate()
    {
        App.CurrentApp.AppSettings.IsUnlockNotificationIgnoredToday = false;
    }

    // INotifyPropertyChanged implementation ---------------------------------------------------
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

}