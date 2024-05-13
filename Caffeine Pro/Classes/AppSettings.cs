using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Caffeine_Pro.Properties;

namespace Caffeine_Pro.Classes;

/// <summary>
/// Settings class for the application
/// </summary>
public sealed class AppSettings : INotifyPropertyChanged
{
    private bool _startActive;
    private bool _noIcon;
    private bool _startWithWindows;
    private Awakeness _startupAwakeness = Awakeness.Default;

    public Awakeness StartupAwakeness
    {
        get => _startupAwakeness;
        set => SetField(ref _startupAwakeness, value);
    }

    /// <summary>
    /// Get/set the value of the start with windows setting
    /// When true, the application will start with windows
    /// </summary>
    public bool StartWithWindows
    {
        get => Routines.IsAddedToWindowsStartup();
        set
        {
            Routines.AddToWindowsStartup(value);
            SetField(ref _startWithWindows, value);
        }
    }

    /// <summary>
    /// Get/set the value of the start inactive setting
    /// When true, the application will start inactive
    /// </summary>
    public bool StartActive
    {
        get => _startActive;
        set => SetField(ref _startActive, value);
    }

    /// <summary>
    /// Get/set the value of the no icon setting
    /// When true, the application will not show an icon in the system tray.
    /// And the only way to interact with the application is through the command line.
    /// </summary>
    public bool NoIcon
    {
        get => _noIcon;
        set => SetField(ref _noIcon, value);
    }

    // INotifyPropertyChanged implementation -----------------------------------
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
        
        //App.PropertiesSettings.Default.StartupAwakeness = (int)value;
        App.CurrentApp.AppSettings.Save();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(this);
        Settings.Default.AppSettings = json;
        Settings.Default.Save();
    }


    public static AppSettings Load()
    {
        var json = Settings.Default.AppSettings;
        if (string.IsNullOrEmpty(json))
        {
            return new AppSettings();
        }

        try
        {
            if (JsonSerializer.Deserialize<AppSettings>(json) is { } settings)
            {
                return settings;
            }
        }
        catch
        {
            // ignored
        }

        return new AppSettings();
    }
}