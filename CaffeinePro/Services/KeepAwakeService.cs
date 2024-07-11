using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using CaffeinePro.Classes;
using Notification.Wpf;
using Timer = System.Timers.Timer;

namespace CaffeinePro.Services;

public sealed class KeepAwakeService : INotifyPropertyChanged
{
    private bool _isActive;
    private Awakeness _awakeness;
    private string _statusText = string.Empty;


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
    public KeepAwakeService(WindowsSessionService windowsSessionService, NotificationManager notificationManager)
    {
        _windowsSessionService = windowsSessionService;
        _windowsSessionService.OnUnlock += (_, _) => OnUnlock();
        _windowsSessionService.OnLock += (_, _) => OnLock();
        _notificationManager = notificationManager;
        _timer.Elapsed += TimerFunction;
        Awakeness = _awakeness = Awakeness.Indefinite;
    }

    private readonly WindowsSessionService _windowsSessionService;
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
    /// The timer that keeps Windows awake
    /// </summary>
    private readonly Timer _timer = new(App.CurrentApp.TimerInterval)
    {
        AutoReset = true,
        Enabled = false,
    };

    private bool _isTemporarilyInactive;
    private bool _isTemporarilyInactiveBecauseSessionLocked;
    private bool _isTemporarilyInactiveBecauseOnBattery;
    private bool _isTemporarilyInactiveBecauseCpuBelowPercentage;

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

        if (Awakeness.EndDateTime < DateTime.Now)
        {
            Awakeness = Awakeness.RenewDateTime(Awakeness);
        }

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

        if (executeAfterwardsAction)
        {
            _windowsSessionService.ExecuteSessionAction(_awakeness.AfterwardsAction);
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
        IsTemporarilyInactiveBecauseSessionLocked =
            Awakeness.Options.InactiveWhenLocked && Routines.IsWorkstationLocked();

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
        Debug.Assert(Awakeness.Options != null, "Awakeness.Options != null");

        UpdateIsTemporarilyInactive();

        if (IsTemporarilyInactive)
        {
            return;
        }

        // Deactivate when the time is up
        if (DateTime.Now >= Awakeness.EndDateTime)
        {
            Deactivate(true);
            return;
        }

        // Handle AllowScreenSaver
        if (Awakeness.Options.AllowScreenSaver)
        {
            // Prevent windows from going to sleep, but allows screen saver
            // This line does *not* prevent other programs from detecting inactivity
            _ = SetThreadExecutionState(EsContinuous | EsSystemRequired);
        }
        else
        {
            // This line allows Windows to go to sleep
            _ = SetThreadExecutionState(EsContinuous);

            // Simulate keypress to prevent Windows from going to sleep
            // This also prevents other programs to detect inactivity
            KeySimulator.PressF15();
        }
    }

    // INotifyPropertyChanged implementation ---------------------------------------------------
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private void OnLock()
    {
        if (Awakeness.Options.InactiveWhenLocked)
        {
            IsTemporarilyInactiveBecauseSessionLocked = true;
        }
    }

    private void OnUnlock()
    {
        IsTemporarilyInactiveBecauseSessionLocked = false;
        ShowUnlockNotification();
    }

    /// <summary>
    /// When unlocking Windows session, show a notification to ask user if the program should be activated
    /// </summary>
    private void ShowUnlockNotification()
    {
        if (IsActive
            || ShouldSkipUnlockNotificationToday()
            || App.CurrentApp.AppSettings.StartActive == false
            || App.CurrentApp.AppSettings.StartupAwakeness.EndDateTime.TimeOfDay < DateTime.Now.TimeOfDay)
        {
            return;
        }


        // Ask user if the program should be activated
        var aw = Awakeness.RenewDateTime(App.CurrentApp.AppSettings.StartupAwakeness);
        
        // Setting the Awakeness to startup Awakeness
        Awakeness = aw;
        
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            var content = new NotificationContent
            {
                Title = App.AppName,
                Message = $"Click Activate to keep your computer awake {aw.GetAwakenessDescription()}.",
                Type = NotificationType.Information,

                LeftButtonContent = "Activate",
                LeftButtonAction = () => Activate(aw), // <- Activate if user clicks Activate

                RightButtonContent = " Ignore for Today ",
                RightButtonAction = SetSkipUnlockNotificationToday, // <- Ignore if user clicks Ignore

                CloseOnClick = true,
                Background = SystemColors.ControlBrush,
                Foreground = SystemColors.ControlTextBrush,
                Icon = App.CurrentApp.InactiveIcon,
            };

            _notificationManager.Show(content, expirationTime: TimeSpan.FromMinutes(10));
        });
    }

    private void SetSkipUnlockNotificationToday()
    {
        App.CurrentApp.AppSettings.IgnoreUnlockNotificationDate = DateTime.Today;
    }

    private bool ShouldSkipUnlockNotificationToday()
    {
        return App.CurrentApp.AppSettings.IgnoreUnlockNotificationDate == DateTime.Today;
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