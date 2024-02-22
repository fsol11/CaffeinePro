using System.Windows;

namespace Caffeine_Pro.Classes;

internal class ParameterProcessorService(KeepAwakeService keepAwakeService)
{
    internal enum StartActions
    {
        Activate,
        Deactivate,
        DoNothing
    }

    public void ProcessArgs(string[]? eArgs, StartActions defaultAction)
    {
        var unrecognizedParameters = string.Empty;
        foreach (var arg in eArgs)
        {
            var tokens = arg.Split(':');
            switch (tokens[0])
            {
                case "-help":
                    MessageBox.Show(Title + Help);
                    Application.Current.Shutdown();
                    break;

                case "exit":
                    Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                    break;

                case "activate":
                    keepAwakeService.Activate();
                    defaultAction = StartActions.Activate;
                    break;

                case "deactivate":
                    keepAwakeService.Deactivate();
                    defaultAction = StartActions.Deactivate;
                    break;

                // ReSharper disable once StringLiteralTypo
                case { } s1 when s1.StartsWith("activefor"):
                    if (int.TryParse(s1[9..], out var minutes))
                        keepAwakeService.ActivateUntil(DateTime.Now.AddMinutes(minutes));
                    break;

                // ReSharper disable once StringLiteralTypo
                case { } s when s.StartsWith("activeuntil"):
                    if (DateTime.TryParse(s[11..], out var untilTime))
                    {
                        var until = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, untilTime.Hour, untilTime.Minute, 0);
                        if (until < DateTime.Now)
                            until = until.AddDays(1);
                        keepAwakeService.ActivateUntil(until);
                    }
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-startinactive":
                    AppSettings.Default.StartInactive = true;
                    defaultAction = StartActions.Deactivate;
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-allowss":
                    AppSettings.Default.AllowScreenSaver = true;
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-noicon":
                    AppSettings.Default.NoIcon = true;
                    break;
                case { Length: > 4 } s when arg.StartsWith("-cpu"):
                    if (int.TryParse(s[4..], out var cpuPercentage))
                    {
                        AppSettings.Default.CpuUsage = cpuPercentage;
                        AppSettings.Default.DeactivateWhenCpuBelowPercentage = true;
                    }
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-deactivatewhenlocked":
                    AppSettings.Default.DeactivateWhenLocked = true;
                    break;

                // ReSharper disable once StringLiteralTypo
                case "-deactivateonbattery":
                    AppSettings.Default.DeactivateOnBattery = true;
                    break;

                default:
                    unrecognizedParameters += arg + "\n";
                    break;
            }
        }

        switch (defaultAction)
        {
            case StartActions.Activate:
                keepAwakeService.Activate();
                break;
            case StartActions.Deactivate:
                keepAwakeService.Deactivate();
                break;
        }

        if (!string.IsNullOrEmpty(unrecognizedParameters))
        {
            MessageBox.Show($"Unrecognized parameters:\n{unrecognizedParameters}\n\n{Help}");
        }
    }

    public static string Title =>
            $"Caffeine Pro Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}\n" +
            $"By Farshid Solimanpour\r\n" +
            $"\r\n";

    public static string Help
    {
        get
        {
            return $"Usage: CaffeinePro.exe [Command] [options]\n" +
                   $"\r\n" +
                   $"Commands:\n" +
                   $"    exit \t\t\texits the instance\r\n" +
                   $"    activate\t\tactivate\r\n" +
                   $"    deactivate\t\tdeactivate\r\n" +
                   $"    activeforX\t\tactivate for X min\r\n" +
                   $"    activeuntilX\t\tactivate until X (X=hh:mmpp)\r\n" +
                   $"\r\n" +
                   $"Saved Options (will be saved for future runs):\r\n" +
                   $"  -help\t\t\tshow help\r\n" +
                   $"  -startinactive\t\tstarts inactive\r\n" +
                   $"  -allowss\t\tallow screen saver. No mouse/key sim\r\n" +
                   $"  -cpuX\t\t\tdeactivate when CPU below X%\r\n" +
                   $"  -deactivatewhenlocked\tdeactivate when computer is locked\r\n" +
                   $"  -deactivateonbattery\tdeactivate when on battery\r\n" +
                   $"\r\n" +
                   $"Unsaved Options:\r\n" +
                   $"  -noicon\t\t\tdo not show icon in system tray\r\n";
        }
    }
}

