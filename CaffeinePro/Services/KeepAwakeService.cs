using System.ComponentModel;
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
            UpdateStatusText();
            OnStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Describes why the service is currently paused (e.g. "On battery"), or an empty
    /// string when it is not paused.
    /// </summary>
    public string TemporarilyInactiveReason
    {
        get => _temporarilyInactiveReason;
        private set
        {
            if (value == _temporarilyInactiveReason)
            {
                return;
            }

            SetField(ref _temporarilyInactiveReason, value);

            // Also refreshed here, and not only from IsTemporarilyInactive, so that a reason
            // swapping over while the service stays paused (unplugging an already locked machine)
            // still reaches the status text.
            UpdateStatusText();
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

    /// <summary>
    /// True while the service is paused, but not because of the battery. The status button shows the
    /// battery icon for a battery pause and its plain paused dot for the rest, so the two must not
    /// light up at the same time when the machine is both locked and unplugged.
    /// </summary>
    public bool IsTemporarilyInactiveOnlyBecauseSessionLocked =>
        IsTemporarilyInactive && !IsTemporarilyInactiveBecauseOnBattery;

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
        SystemActivityService systemActivityService, NotificationManager notificationManager,
        AppSettings appSettings)
    {
        _windowsSessionService = windowsSessionService;

        // SystemEvents raises session switches on its own dedicated thread. Everything below ends up
        // installing the low-level keyboard/mouse hooks and touching WPF state, both of which belong
        // on the UI thread, so the handlers are marshalled there.
        _windowsSessionService.OnUnlock += (_, _) => RunOnUiThread(OnUnlock);
        _windowsSessionService.OnLock += (_, _) => RunOnUiThread(OnLock);
        _systemActivityService = systemActivityService;
        _systemActivityService.OnUserBecameActive += (_, _) => RunOnUiThread(OnUserBecameActive);

        // Without this the pause would only be noticed on the next timer tick, i.e. up to two
        // minutes after the charger was pulled out.
        _systemActivityService.OnPowerSourceChanged += (_, _) => RunOnUiThread(UpdateIsTemporarilyInactive);
        _notificationManager = notificationManager;
        _appSettings = appSettings;
        _appSettings.PropertyChanged += OnAppSettingsChanged;
        _timer.Elapsed += TimerFunction;

        Awakeness = _awakeness = Awakeness.Indefinite;
    }

    private readonly WindowsSessionService _windowsSessionService;
    private readonly SystemActivityService _systemActivityService;
    private readonly NotificationManager _notificationManager;
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Runs the given action on the UI thread. Session-switch, power and timer callbacks all arrive
    /// on background threads, but activating/deactivating touches the window hooks and the UI.
    /// </summary>
    private static void RunOnUiThread(Action action)
    {
        var dispatcher = App.CurrentApp?.Dispatcher;

        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        // SessionLogoff and the expiry of an awakeness that shuts the machine down both arrive while
        // the dispatcher may already be tearing down, where dispatching throws.
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            return;
        }

        try
        {
            dispatcher.Invoke(action);
        }
        catch (TaskCanceledException)
        {
            // The dispatcher shut down while the callback was queued - there is nothing left to update.
        }
    }

    /// <summary>
    /// Re-evaluates the temporarily-inactive state as soon as a related option changes,
    /// so the UI reflects the new battery setting immediately instead of waiting for the next timer tick.
    /// </summary>
    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.InactiveWhenOnBattery))
        {
            UpdateIsTemporarilyInactive();
        }
    }

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
        if (!IsActive)
        {
            return;
        }

        StatusText += $" - {Awakeness.GetAwakenessDescription()}";

        if (IsTemporarilyInactive)
        {
            StatusText += $"\r\n{TemporarilyInactiveReason}";
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
    /// Applies the given execution state flags on the UI thread.
    /// </summary>
    /// <remarks>
    /// Windows tracks the execution state per thread, and the timer callbacks that set it run on
    /// arbitrary thread pool threads. Every call therefore has to be funnelled through one fixed
    /// thread, otherwise a later ES_CONTINUOUS meant to release the state applies to an unrelated
    /// thread and leaves the machine pinned awake - while pausing or deactivating.
    /// </remarks>
    private static void SetExecutionState(uint flags) => RunOnUiThread(() => SetThreadExecutionState(flags));

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
    private string _temporarilyInactiveReason = string.Empty;

    private const string OnBatteryReason = "Paused - running on battery";
    private const string SessionLockedReason = "Paused - workstation locked";

    /// <summary>
    /// Activate the keep awake service to the default time
    /// </summary>
    public void ActivateDefault()
    {
        if (App.CurrentApp.AppSettings.StartupAwakeness is not { } startupAwakeness)
        {
            return;
        }

        // Always renewed: the end time stored in the settings was calculated when they were loaded,
        // so a relative startup awakeness ("for 2 hours") has to be recounted from now.
        Activate(Awakeness.RenewDateTime(startupAwakeness));
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

        // Evaluated right away so that activating while already on battery (or with the screen
        // locked) shows up as paused immediately rather than on the first timer tick.
        UpdateIsTemporarilyInactive();
    }

    /// <summary>
    /// Deactivate the keep awake service
    /// </summary>
    public void Deactivate(bool executeAfterwardsAction = false)
    {
        if (_isActive)
        {
            SetExecutionState(EsContinuous); // <- Setting thread state to normal
            IsActive = false;
        }

        _windowsKeyboardMouseCapture.Unhook();

        if (executeAfterwardsAction)
        {
            WindowsSessionService.ExecuteSessionAction(App.CurrentApp.AppSettings.AfterwardsAction);
        }
    }

    /// <summary>
    /// Updates status of the temporarily inactive flags
    /// </summary>
    private void UpdateIsTemporarilyInactive()
    {
        IsTemporarilyInactiveBecauseOnBattery = _appSettings.InactiveWhenOnBattery && Routines.IsOnBattery();
        IsTemporarilyInactiveBecauseSessionLocked = Routines.IsWorkstationLocked();

        // Set before the aggregate flag below, whose setter refreshes the status text from it.
        TemporarilyInactiveReason = IsTemporarilyInactiveBecauseOnBattery
            ? OnBatteryReason
            : IsTemporarilyInactiveBecauseSessionLocked
                ? SessionLockedReason
                : string.Empty;

        IsTemporarilyInactive = IsTemporarilyInactiveBecauseOnBattery
                                || IsTemporarilyInactiveBecauseSessionLocked;

        // Computed from the two flags above, so it has to be announced by hand.
        OnPropertyChanged(nameof(IsTemporarilyInactiveOnlyBecauseSessionLocked));

        if (IsTemporarilyInactive)
        {
            // Pausing has to hand the machine back to Windows as well: an ES_SYSTEM_REQUIRED set on
            // an earlier tick (AllowScreenSaver mode) stays in effect until it is explicitly
            // cleared, so without this the battery would keep draining while the status already
            // reads as paused.
            SetExecutionState(EsContinuous);
        }
    }

    /// <summary>
    /// The function that is called at each timer tick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="elapsedEventArgs"></param>
    private void TimerFunction(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        UpdateIsTemporarilyInactive();

        if (IsTemporarilyInactive)
        {
            return;
        }

        // Deactivate when the time is up. The timer runs on a thread pool thread, while unhooking the
        // keyboard/mouse hooks and running the afterwards action belong on the UI thread.
        if (Awakeness.GetNow() >= Awakeness.EndDateTime)
        {
            RunOnUiThread(() => Deactivate(true));
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
            SetExecutionState(EsContinuous | EsSystemRequired);
        }
        else
        {
            // Allow Windows to manage sleep normally (SendInput below keeps it awake indirectly
            // by resetting the idle timer through actual simulated input).
            SetExecutionState(EsContinuous);

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
        // Recomputed as a whole so the aggregate IsTemporarilyInactive (and with it the tray icon and
        // the status text) is updated right away instead of on the next timer tick - which never comes
        // while the service is inactive, because the timer is stopped then.
        UpdateIsTemporarilyInactive();
    }

    private void OnUnlock()
    {
        UpdateIsTemporarilyInactive();
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
        var settings = App.CurrentApp.AppSettings;

        if (IsActive
            || settings.IsUnlockNotificationIgnoredToday
            || settings.StartActive == false
            || settings.StartupAwakeness is not { } startupAwakeness)
        {
            return;
        }

        // For an absolute startup time ("until 5 PM") there is nothing left to do once that time has
        // passed today. Relative ("for 2 hours") and indefinite awakenesses always stay valid - their
        // stored EndDateTime must not be used here, as it dates back to when the settings were loaded.
        if (!startupAwakeness.IsIndefinite
            && !startupAwakeness.IsRelative
            && startupAwakeness.RelativeSpan <= Awakeness.GetTimeOfDay())
        {
            return;
        }

        if (settings.StartActive == true)
        {
            ActivateDefault();
            return;
        }

        // Ask user if the program should be activated
        //   When reaching here => App.CurrentApp.AppSettings.StartActive is null
        //   Which means user has selected "Ask Me"
        NotificationWindow.OpenIt(Awakeness.RenewDateTime(startupAwakeness));
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