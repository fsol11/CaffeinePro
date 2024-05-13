using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Timers;
using Caffeine_Pro.Classes;
using Timer = System.Timers.Timer;

namespace Caffeine_Pro.Services;

public sealed class KeepAwakeService : INotifyPropertyChanged
{
    private bool _isActive;
    private Awakeness _awakeness;
    private string _statusText = string.Empty;

    public Awakeness Awakeness
    {
        get => _awakeness;
        set
        {
            SetField(ref _awakeness, value);
            UpdateStatusText();
            Activate();
            OnStatusChanged?.Invoke(this, EventArgs.Empty);
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
    public KeepAwakeService(WindowsSessionService windowsSessionService)
    {
        WindowsSessionService = windowsSessionService;
        _timer.Elapsed += TimerFunction;
        Awakeness = _awakeness = new Awakeness();
    }

    private WindowsSessionService WindowsSessionService
    {
        get;
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
            SetField(ref _isActive, value);
            UpdateStatusText();
            OnStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateStatusText()
    {
        StatusText =
            Assembly.GetExecutingAssembly().GetName().Name + " - " +
            //"Caffeine Pro - " +
            (IsActive ? "Active" : "Inactive") +
            " " +
            Awakeness.StatusText;
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
    private readonly Timer _timer = new(59000) // 59 seconds
    {
        AutoReset = true,
        Enabled = false,
    };

    /// <summary>
    /// Activate the keep awake service until a specific date and time
    /// </summary>
    /// <param name="awakeness"></param>
    public void Activate(Awakeness? awakeness = null)
    {
        if(awakeness != null)
        {
            Awakeness = awakeness;

            if (awakeness.UntilDateTime == DateTime.MinValue) // <- set as inactive
            {
                Deactivate();
                return;
            }
        }

        _timer.Start();
        IsActive = true;
    }


    /// <summary>
    /// Deactivate the keep awake service
    /// </summary>
    public void Deactivate(bool executeAfterwardsAction = false)
    {
        if (_isActive)
        {
            _ = SetThreadExecutionState(EsContinuous);
            _timer.Stop();
            IsActive = false;
        }

        if (executeAfterwardsAction)
        {
            WindowsSessionService.ExecuteSessionAction(_awakeness.AfterwardsAction);
        }
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

        if (Awakeness.Options.DeactivateWhenLocked && Routines.IsWorkstationLocked())
        {
            Deactivate(true);
            return;
        }

        // Handle DeactivateOnBattery
        if (Awakeness.Options.DeactivateWhenOnBattery && Routines.IsOnBattery())
        {
            Deactivate(true);
            return;
        }

        // Handle DeactivateWhenCpuBelowPercentage
        if (Awakeness.Options.DeactivateWhenCpuBelowPercentage &&
            Routines.CpuUsage() < Awakeness.Options.CpuBelowPercentage)
        {
            Deactivate(true);
            return;
        }

        // Deactivate when the time is up
        if (DateTime.Now >= Awakeness.UntilDateTime)
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