using System.Windows;
using Caffeine_Pro.Services;

namespace Caffeine_Pro.Classes;

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

    /// <summary>
    /// Processes the command line arguments
    /// </summary>
    public void ProcessArgs(string[] eArgs)
    {
        var unrecognizedParameters = string.Empty;
        var awakeness = new Awakeness();
        var action = StartActions.DoNothing;
        foreach (var arg in eArgs)
        {
            switch (arg.Trim().ToLower())
            {
                case "exit":
                    Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                    return;

                case "activate":
                    awakeness = Awakeness.Indefinite;
                    action = StartActions.Activate;
                    break;

                case "deactivate":
                    keepAwakeService.Deactivate();
                    action = StartActions.Deactivate;
                    break;

                case "-help":
                    MessageBox.Show(Title + Help);
                    Application.Current.Shutdown();
                    break;

                case "-status":
                    MessageBox.Show((Application.Current as App)!.KeepAwakeService.StatusText);
                    break;
                
                // ReSharper disable once StringLiteralTypo
                case { } s1 when s1.StartsWith("activefor") && s1.Length > 9:
                    if (int.TryParse(s1[9..], out var minutes))
                    {
                        awakeness = new Awakeness(DateTime.Now.AddMinutes(minutes));
                    }

                    break;

                // ReSharper disable once StringLiteralTypo
                case { } s when s.StartsWith("activeuntil") && s.Length > 11:
                    if (DateTime.TryParse(s[11..], out var untilTime))
                    {
                        var until = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, untilTime.Hour, untilTime.Minute, 0);
                        if (until < DateTime.Now)
                        {
                            until = until.AddDays(1);
                        }
                        awakeness = new Awakeness(until);
                    }
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-allowss":
                    awakeness.Options.AllowScreenSaver = true;
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-noicon":
                    (Application.Current as App)!.AppSettings.NoIcon = true;
                    break;

                case "-icon":
                    (Application.Current as App)!.AppSettings.NoIcon = false;
                    break;

                case { } s when arg.StartsWith("-cpu") && s.Length > 4:
                    if (int.TryParse(s[4..], out var cpuPercentage))
                    {
                        awakeness.Options.CpuBelowPercentage = cpuPercentage;
                        awakeness.Options.DeactivateWhenCpuBelowPercentage = true;
                    }
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-deactivewhenlocked":
                    awakeness.Options.DeactivateWhenLocked = true;
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-deactivateonbattery":
                    awakeness.Options.DeactivateWhenOnBattery = true;
                    break;

                default:
                    unrecognizedParameters += arg + "\n";
                    break;
            }
        }

        if (!string.IsNullOrEmpty(unrecognizedParameters))
        {
            MessageBox.Show($"Unrecognized parameters:\n{unrecognizedParameters}\n\n{Help}");
            return;
        }        
        
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
        $"  -cpuX\t\t\tdeactivate when CPU below X% (default: false)\r\n" +
        $"  -deactivatewhenlocked\tdeactivate when computer is locked (default: true)\r\n" +
        $"  -deactivateonbattery\tdeactivate when on battery (default: false)\r\n" +
        $"  -noicon\t\t\tdo not show the icon in system tray (default: false)\r\n" +
        $"  -icon\t\t\tshow the icon in system tray (default: true)\r\n";
}

