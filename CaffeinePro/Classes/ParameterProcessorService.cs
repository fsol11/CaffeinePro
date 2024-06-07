using System.Windows;
using CaffeinePro.Services;

namespace CaffeinePro.Classes;

/// <summary>
/// This class processes the command line arguments
/// </summary>
/// <param name="keepAwakeService"></param>
public class ParameterProcessorService(KeepAwakeService keepAwakeService)
{
    /// <summary>
    /// Start actions for the command processor. This important when the application is sending
    /// commands to the running instance vs when it is starting the first instance.
    /// </summary>
    private enum StartActions
    {
        Activate,
        Deactivate,
        DoNothing
    }

    public void ShowHelpAndExitIfRequested(string[] args)
    {
        if (Has(args, "-help"))
        {
            MessageBox.Show(Title + Help);
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Processes the command line arguments
    /// </summary>
    public void ProcessArgs(string[] eArgs)
    {
        var unrecognizedParameters = string.Empty;
        var action = StartActions.DoNothing;
        var timespan = TimeSpan.Zero;
        var options = new AwakenessOptions();
        var type = Awakeness.AwakenessTypes.Absolute;
        var afterwards = SessionAction.None;


        if (Has(eArgs, "exit"))
        {
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            return;
        }

        if (Has(eArgs, "activate"))
        {
            action = StartActions.Activate;
        }
        else if (Has(eArgs, "deactivate"))
        {
            action = StartActions.Deactivate;
        }

        if (Has(eArgs, "-status"))
        {
            MessageBox.Show((Application.Current as App)!.KeepAwakeService.StatusText);
        }

        if (Has(eArgs, "-activeFor"))
        {
            type = Awakeness.AwakenessTypes.Relative;
            var s = Item(eArgs, "activeFor");
            if (s.Length > 9 && int.TryParse(s[9..], out var minutes))
            {
                timespan = TimeSpan.FromMinutes(minutes);
            }
        }

        if (Has(eArgs, "-ActiveUntil"))
        {
            type = Awakeness.AwakenessTypes.Absolute;
            var s = Item(eArgs, "activeUntil");
            if (s.Length > 11 && DateTime.TryParse(s[11..], out var untilTime))
            {
                timespan = untilTime.TimeOfDay;
            }
        }

        if (Has(eArgs, "-allowSS"))
        {
            options = new AwakenessOptions(
                true,
                options.InactiveWhenLocked,
                options.InactiveWhenOnBattery,
                options.InactiveWhenCpuBelowPercentage,
                options.CpuBelowPercentage);
        }

        if (Has(eArgs, "-cpu"))
        {
            var s = Item(eArgs, "-cpu");
            if (s.Length > 4 && int.TryParse(s[4..], out var cpuPercentage))
            {
                options = new AwakenessOptions(
                    options.AllowScreenSaver,
                    options.InactiveWhenLocked,
                    options.InactiveWhenOnBattery,
                    true,
                    cpuPercentage);
            }
        }

        if (Has(eArgs, "-inactiveWhenLocked"))
        {
            options = new AwakenessOptions(
                options.AllowScreenSaver,
                true,
                options.InactiveWhenOnBattery,
                options.InactiveWhenCpuBelowPercentage,
                options.CpuBelowPercentage);
        }

        if (Has(eArgs, "-inactiveOnBattery"))
        {
            options = new AwakenessOptions(
                options.AllowScreenSaver,
                options.InactiveWhenLocked,
                true,
                options.InactiveWhenCpuBelowPercentage,
                options.CpuBelowPercentage);
        }

        if (!string.IsNullOrEmpty(unrecognizedParameters))
        {
            MessageBox.Show($"Unrecognized parameters:\n{unrecognizedParameters}\n\n{Help}");
            return;
        }

        var awakeness = new Awakeness(type, timespan, options, afterwards);

        switch (action)
        {
            case StartActions.Activate:
                keepAwakeService.Activate(awakeness);
                break;
            case StartActions.Deactivate:
                keepAwakeService.Deactivate();
                break;
        }
    }

    private static bool Has(IEnumerable<string> args, string arg) =>
        args.Any(a => a.StartsWith(arg, StringComparison.CurrentCultureIgnoreCase));

    private static string Item(IEnumerable<string> args, string arg) =>
        args.First(a => a.StartsWith(arg, StringComparison.CurrentCultureIgnoreCase));

    /// <summary>
    /// Title of the application
    /// </summary>
    private static string Title =>
        $"Caffeine Pro Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}\n" +
        $"By Farshid Solimanpour (caffeinepro@farshid.ca)\r\n" +
        $"\r\n";

    /// <summary>
    /// Help message
    /// </summary>
    private static string Help =>
        $"Usage: CaffeinePro [Command] [options]\n" +
        $"\r\n" +
        $"Commands:\n" +
        $"    activate\t\tactivate (default)\r\n" +
        $"    activeforX\t\tactivate for X min\r\n" +
        $"    activeuntilX\t\tactivate until X (X=hh:mmpp)\r\n" +
        $"    deactivate\t\tdeactivate the previous instance\r\n" +
        $"    exit \t\t\texits the previous instance\r\n" +
        $"\r\n" +
        $"Options:\r\n" +
        $"  -help\t\t\tshow help\r\n" +
        $"  -resetoptions\t\treset options to default values\r\n" +
        $"  -saveoptions\t\tsave options as default for next runs\r\n" +
        $"  -startinactive\t\tstarts inactive\r\n" +
        $"\r\n" +
        $"  -allowss\t\tallow screen saver. No mouse/key sim (default: false)\r\n" +
        $"  -cpuX\t\t\tinactive when CPU below X% (default: false)\r\n" +
        $"  -inactivewhenlocked\tinactive when computer is locked (default: false)\r\n" +
        $"  -inactiveOnBattery\tinactive when on battery (default: false)\r\n";
}

