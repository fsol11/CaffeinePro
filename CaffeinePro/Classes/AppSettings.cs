// -----------------------------------------------------------------------
// <copyright file="AppSettings.cs" company="Lotrasoft Inc.">
//     Copyright (c) 2026 Lotrasoft Inc. All rights reserved.
// </copyright>
// <author>Farshid Solimanpour</author>
// -----------------------------------------------------------------------
// This file is part of the Caffeine Pro project.
// The Caffeine Pro project is licensed under MIT License.
// For more details, see the LICENSE file in the project root.
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using System.Text.Json.Serialization;
using CaffeinePro.Localization;
using CaffeinePro.Services;

namespace CaffeinePro.Classes;

/// <summary>
/// Represents the settings for the application.
/// </summary>
public sealed class AppSettings : INotifyPropertyChanged
{
    // Fields for storing the settings values
    // null is the default: the user is asked at startup/unlock whether to activate.
    private bool? _startActive = null;
    private bool _isLoading = true;
    private bool _startWithWindows;
    private bool _allowScreenSaver;
    private bool _inactiveWhenOnBattery;
    private SessionAction _afterwardsAction = SessionAction.None;
    private Awakeness _startupAwakeness = Awakeness.Indefinite;
    private DateTime _ignoreUnlockNotificationDate = DateTime.MaxValue;
    private HotKey _blackoutHotKey = HotKey.DefaultBlackout;
    private string _language = LocalizationService.SystemDefaultCode;
    private static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CaffeinePro", "CaffeineProConfig.json");

    public static AppSettings Load()
    {
        if (!File.Exists(ConfigPath))
        {
            return new AppSettings
            {
                _isLoading = false
            };
        }

        var s = JsonSerializer.Deserialize(File.OpenRead(ConfigPath), AppSettingsJsonContext.Default.AppSettings) ?? new AppSettings();
        s._isLoading = false;
        return s;

    }

    /// <summary>
    /// Gets or sets the awakeness setting at startup.
    /// </summary>
    [JsonInclude]
    public Awakeness StartupAwakeness
    {
        get => _startupAwakeness;
        set => SetField(ref _startupAwakeness, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application starts with Windows.
    /// </summary>
    [JsonInclude]
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
    /// Gets or sets a value indicating whether the screen saver is allowed to run while
    /// keeping the system awake. When enabled, the system is kept awake without simulating
    /// input, so inactivity is still detectable by communication software.
    /// </summary>
    [JsonInclude]
    public bool AllowScreenSaver
    {
        get => _allowScreenSaver;
        set => SetField(ref _allowScreenSaver, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether keeping awake is paused while the system is on battery.
    /// </summary>
    [JsonInclude]
    public bool InactiveWhenOnBattery
    {
        get => _inactiveWhenOnBattery;
        set => SetField(ref _inactiveWhenOnBattery, value);
    }

    /// <summary>
    /// Gets or sets the action performed after an active timer expires.
    /// </summary>
    [JsonInclude]
    public SessionAction AfterwardsAction
    {
        get => _afterwardsAction;
        set => SetField(ref _afterwardsAction, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application starts in active state.
    /// <c>true</c> auto-activates, <c>false</c> stays inactive, and <c>null</c> (the default)
    /// asks the user at startup and unlock.
    /// </summary>
    [JsonInclude]
    public bool? StartActive
    {
        get => _startActive;
        set => SetField(ref _startActive, value);
    }

    /// <summary>
    /// Gets or sets the system-wide shortcut that blacks out every screen, making the machine look
    /// switched off until Escape is pressed. <see cref="HotKey.None"/> disables the shortcut.
    /// </summary>
    [JsonInclude]
    public HotKey BlackoutHotKey
    {
        get => _blackoutHotKey;
        set => SetField(ref _blackoutHotKey, value);
    }

    /// <summary>
    /// Gets or sets the language the interface is shown in, as an
    /// <see cref="AppLanguage.Code"/>. <see cref="LocalizationService.SystemDefaultCode"/> - the
    /// default - follows the Windows display language.
    /// </summary>
    [JsonInclude]
    public string Language
    {
        get => _language;
        set => SetField(ref _language, value);
    }

    /// <summary>
    /// Gets or sets the date to ignore unlock notifications.
    /// </summary>
    [JsonIgnore]
    public DateTime IgnoreUnlockNotificationDate
    {
        get => _ignoreUnlockNotificationDate;
        set => SetField(ref _ignoreUnlockNotificationDate, value);
    }

    [JsonIgnore]
    public bool IsUnlockNotificationIgnoredToday
    {
        get => IgnoreUnlockNotificationDate == DateTime.Today;
        set
        {
            IgnoreUnlockNotificationDate = value ? DateTime.Today : DateTime.MinValue;
            OnPropertyChanged(nameof(IsUnlockNotificationIgnoredToday));
        }
    }

    /// <summary>
    /// Re-announces the settings whose displayed form is translated, so their bindings pick up a
    /// language change: <see cref="BlackoutHotKey"/> spells the modifier names out in words, and
    /// the startup awakeness caches the texts it is shown as.
    /// </summary>
    public void RefreshLocalizedTexts()
    {
        StartupAwakeness.UpdateTexts();
        OnPropertyChanged(nameof(StartupAwakeness));
        OnPropertyChanged(nameof(BlackoutHotKey));
    }

    // INotifyPropertyChanged implementation -----------------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the field to the given value and raises the PropertyChanged event.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The field to set.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null, bool save = true)
    {
        if (EqualityComparer<T>.Default.Equals(field, value) || propertyName == null)
        {
            return;
        }

        //SetSettings(propertyName, value); // Saving the setting
        field = value;
        if (save)
        {
            Save();
        }

        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Saves the current settings to a JSON string and stores it in the application settings.
    /// </summary>
    private void Save()
    {
        if (_isLoading)
        {
            return;
        }

        var json = JsonSerializer.Serialize(this, AppSettingsJsonContext.Default.AppSettings);
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            App.CurrentApp.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                Dialogs.Show(ex.Message, LocalizationService.Get("Error_SavingSettingsTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }
    }
}

