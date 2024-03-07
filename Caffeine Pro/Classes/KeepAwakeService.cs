using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Caffeine_Pro.Classes;

public class KeepAwakeService : INotifyPropertyChanged
{
    // Property backing fields
    private bool _isActive;
    private string _statusText = string.Empty;
    private string _untilDateTimeText = string.Empty;
    private DateTime _untilDateTime = DateTime.MinValue;

    /// <summary>
    /// get/set activation status of the keep awake service
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    /// <summary>
    /// This event is called when keep awake timer is enabled or disabled
    /// </summary>
    public event EventHandler? OnStatusChanged;

    /// <summary>
    /// Declares the Windows function for setting thread properties
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;

    /// <summary>
    /// Get/set date and time of the moment when the keep awake service will be disabled
    /// When it is set to DateTime.MaxValue, the service will be active until it is manually disabled
    /// </summary>
    public DateTime UntilDateTime
    {
        get => _untilDateTime;
        private set
        {
            SetField(ref _untilDateTime, value);
            UntilDateTimeText = Routines.GetTimeString(value);
        }
    }

    /// <summary>
    /// Get the text representation of the date and time of the moment when the keep awake service will be disabled
    /// This is used to display the date and time in the UI
    /// </summary>
    public string UntilDateTimeText
    {
        get => _untilDateTimeText;
        private set => SetField(ref _untilDateTimeText, value);
    }

    /// <summary>
    /// Get status of the keep awake service used for displaying in the UI
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    /// <summary>
    /// The timer that keeps Windows awake
    /// </summary>
    private readonly Timer _timer = new(59000) // 59 seconds
    {
        AutoReset = true,
        Enabled = false,
    };

    /// <summary>
    /// Constructor
    /// </summary>
    public KeepAwakeService()
    {
        _timer.Elapsed += TimerFunction;
    }

    /// <summary>
    /// Activate the keep awake service until it is manually disabled
    /// </summary>
    public void Activate()
    {
        ActivateUntil(DateTime.MaxValue);
    }

    /// <summary>
    /// Activate the keep awake service until a specific date and time
    /// </summary>
    /// <param name="dateTime"></param>
    public void ActivateUntil(DateTime dateTime)
    {
        UntilDateTime = dateTime;
        _timer.Start();
        IsActive = true;
        UpdateStatusText();
        OnStatusChanged?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Deactivate the keep awake service
    /// </summary>
    public void Deactivate()
    {
        SetThreadExecutionState(EsContinuous);
        _timer.Stop();
        IsActive = false;
        UpdateStatusText();
        OnStatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateStatusText()
    {
        StatusText =
                "Caffeine Pro - " +
                (IsActive ? "Active" : "Inactive") +
                (IsActive && UntilDateTime != DateTime.MaxValue ? " until " + UntilDateTimeText : string.Empty)
            ;
    }

    /// <summary>
    /// The function that is called at each timer tick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="elapsedEventArgs"></param>
    private void TimerFunction(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        // Handle DeactivateWhenLocked
        if (!App.AppSettings.ActiveWhenLocked && Routines.IsWorkstationLocked())
        {
            Deactivate();
            return;
        }

        // Handle DeactivateOnBattery
        if (App.AppSettings.DeactivateOnBattery && Routines.IsOnBattery())
        {
            Deactivate();
            return;
        }

        // Handle DeactivateWhenCpuBelowPercentage
        if (App.AppSettings.DeactivateWhenCpuBelowPercentage &&
            Routines.CpuUsage() < App.AppSettings.CpuUsage)
        {
            Deactivate();
            return;
        }

        // Handle AllowScreenSaver
        if (App.AppSettings.AllowScreenSaver)
        {
            // Prevent windows from going to sleep, but allows screen saver
            // This line does *not* prevent other programs from detecting inactivity
            SetThreadExecutionState(EsContinuous | EsSystemRequired);
        }
        else
        {
            // This line allows Windows to go to sleep
            SetThreadExecutionState(EsContinuous);

            // Simulate keypress to prevent Windows from going to sleep
            // This also prevents other programs to detect inactivity
            KeySimulator.PressF15();
        }

        // Deactivate when the time is up
        if (DateTime.Now >= UntilDateTime) Deactivate();
    }

    // INotifyPropertyChanged implementation ---------------------------------------------------
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
