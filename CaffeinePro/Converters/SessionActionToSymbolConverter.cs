using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Services;
using Wpf.Ui.Controls;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a SessionAction to the SymbolRegular icon used for it elsewhere in the UI
/// </summary>
public class SessionActionToSymbolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SessionAction action)
        {
            return SymbolRegular.Empty;
        }

        return action switch
        {
            SessionAction.None => SymbolRegular.Prohibited20,
            SessionAction.Lock => SymbolRegular.LockClosed20,
            SessionAction.SignOut => SymbolRegular.PersonArrowLeft20,
            SessionAction.ForceSignOut => SymbolRegular.PersonSubtract20,
            SessionAction.Exit => SymbolRegular.ArrowExit20,
            SessionAction.Sleep => SymbolRegular.WeatherMoon20,
            SessionAction.Hibernate => SymbolRegular.WeatherSnowflake20,
            SessionAction.Shutdown => SymbolRegular.Power20,
            SessionAction.ForceShutdown => SymbolRegular.Flash20,
            SessionAction.Restart => SymbolRegular.ArrowCounterclockwise20,
            SessionAction.ForceRestart => SymbolRegular.ArrowCounterclockwiseDashes20,
            SessionAction.MonitorOff => SymbolRegular.DesktopOff20,
            _ => SymbolRegular.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
