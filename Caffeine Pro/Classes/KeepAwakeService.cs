using System.Runtime.InteropServices;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Caffeine_Pro.Classes;

internal class KeepAwakeService
{
    public event EventHandler? OnDeactivate;
    public event EventHandler? OnActivate;
    public event EventHandler? Tick;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;

    public DateTime UntilDateTime { get; private set; } = DateTime.MinValue;

    public bool IsActive => _timer.Enabled;

    private readonly Timer _timer = new(59000) // 59 seconds
    {
        AutoReset = true,
        Enabled = false,
    };

    public KeepAwakeService()
    {
        _timer.Elapsed += TimerFunction;
    }

    public void Activate()
    {
        ActivateUntil(DateTime.MaxValue);
    }

    public void ActivateUntil(DateTime dateTime)
    {
        UntilDateTime = dateTime;
        _timer.Start();
        OnActivate?.Invoke(this, EventArgs.Empty);
    }

    public void Deactivate()
    {
        SetThreadExecutionState(EsContinuous);
        _timer.Stop();
        OnDeactivate?.Invoke(this, EventArgs.Empty);
    }


    private void TimerFunction(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        if (AppSettings.Default.DeactivateWhenLocked && Routines.IsWorkstationLocked())
        {
            Deactivate();
            return;
        }

        if (AppSettings.Default.DeactivateOnBattery && Routines.IsOnBattery())
        {
            Deactivate();
            return;
        }

        if (AppSettings.Default.DeactivateWhenCpuBelowPercentage &&
            Routines.CpuUsage() < AppSettings.Default.CpuUsage)
        {
            Deactivate();
            return;
        }

        if (AppSettings.Default.AllowScreenSaver)
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

        Tick?.Invoke(this, EventArgs.Empty);

        if (DateTime.Now >= UntilDateTime) Deactivate();
    }
}
