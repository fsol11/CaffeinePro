using System.Reflection;
using System.Windows;
using CaffeinePro.Localization;
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
            Dialogs.Show(Title + Help, LocalizationService.Get("App_Name"));
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
        var type = Awakeness.AwakenessTypes.Absolute;


        if (Has(eArgs, "exit") || Has(eArgs, "quit"))
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
            Dialogs.Show((Application.Current as App)!.KeepAwakeService.StatusText,
                LocalizationService.Get("App_Name"));
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
            App.CurrentApp.AppSettings.AllowScreenSaver = true;
        }

        if (Has(eArgs, "-inactiveOnBattery"))
        {
            App.CurrentApp.AppSettings.InactiveWhenOnBattery = true;
        }

        if (!string.IsNullOrEmpty(unrecognizedParameters))
        {
            Dialogs.Show(
                LocalizationService.Format("Cli_UnrecognizedParametersFormat", unrecognizedParameters)
                + Environment.NewLine + Environment.NewLine + Help,
                LocalizationService.Get("App_Name"));
            return;
        }

        var awakeness = new Awakeness(type, timespan);

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
    /// The banner shown above the help text.
    /// </summary>
    private static string Title =>
        LocalizationService.Format("Cli_VersionFormat",
            Assembly.GetExecutingAssembly().GetName().Version) + Environment.NewLine +
        LocalizationService.Format("Cli_ByFormat", "Farshid Solimanpour", "caffeinepro@farshid.ca") +
        Environment.NewLine + Environment.NewLine;

    /// <summary>
    /// Help message. Only the descriptions are translated: the commands and switches themselves are
    /// what the user has to type, so they stay exactly as the parser expects them.
    /// </summary>
    private static string Help =>
        LocalizationService.Get("Cli_Usage") + Environment.NewLine + Environment.NewLine +
        LocalizationService.Get("Cli_Commands") + Environment.NewLine +
        Line("activate", "Cli_Cmd_Activate") +
        Line("activeforX", "Cli_Cmd_ActiveFor") +
        Line("activeuntilX", "Cli_Cmd_ActiveUntil") +
        Line("deactivate", "Cli_Cmd_Deactivate") +
        Line("exit", "Cli_Cmd_Exit") +
        Environment.NewLine +
        LocalizationService.Get("Cli_Options") + Environment.NewLine +
        Line("-help", "Cli_Opt_Help") +
        Line("-resetoptions", "Cli_Opt_ResetOptions") +
        Line("-saveoptions", "Cli_Opt_SaveOptions") +
        Line("-startinactive", "Cli_Opt_StartInactive") +
        Line("-allowss", "Cli_Opt_AllowSs") +
        Line("-inactiveOnBattery", "Cli_Opt_InactiveOnBattery");

    /// <summary>
    /// One help line: the literal switch padded to a fixed width, then its translated description.
    /// Padded rather than tabbed, so a description of any length still lines up.
    /// </summary>
    private static string Line(string token, string descriptionKey) =>
        $"  {token.PadRight(20)}{LocalizationService.Get(descriptionKey)}" + Environment.NewLine;
}

