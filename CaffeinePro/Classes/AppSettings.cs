// -----------------------------------------------------------------------
// <copyright file="AppSettings.cs" company="Lotrasoft Inc.">
//     Copyright (c) 2024 Lotrasoft Inc. All rights reserved.
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
using System.Text.Json.Serialization;
using System.Windows.Threading;
using System.Windows;

namespace CaffeinePro.Classes;

/// <summary>
/// Represents the settings for the application.
/// </summary>
public sealed class AppSettings(bool isLoading) : INotifyPropertyChanged
{
    // Fields for storing the settings values
    private bool _startActive;
    private bool _startWithWindows;
    private Awakeness _startupAwakeness = Awakeness.Indefinite;
    private DateTime _ignoreUnlockNotificationDate = DateTime.MaxValue;

    public AppSettings() : this(true)
    {
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
    /// Gets or sets a value indicating whether the application starts in active state.
    /// </summary>
    [JsonInclude]
    public bool StartActive
    {
        get => _startActive;
        set => SetField(ref _startActive, value);
    }

    /// <summary>
    /// Gets or sets the date to ignore unlock notifications.
    /// </summary>
    public DateTime IgnoreUnlockNotificationDate
    {
        get => _ignoreUnlockNotificationDate;
        set => SetField(ref _ignoreUnlockNotificationDate, value);
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
    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);

        // Save the settings when a property changes
        // The following check is necessary to prevent saving the settings when they are being loaded
        if (!isLoading)
        {
            App.CurrentApp.AppSettings.Save();
        }
    }

    private static string GetSettingsFilename()
    {
        return Path.ChangeExtension(Routines.GetExePath(), "settings");
    }

    /// <summary>
    /// Saves the current settings to a JSON string and stores it in the application settings.
    /// </summary>
    private void Save()
    {
        if (isLoading)
        {
            return;
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            IncludeFields = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            IgnoreReadOnlyFields = true,
            PropertyNameCaseInsensitive = true,
        };
        var json = JsonSerializer.Serialize(this, options);
        try
        {
            File.WriteAllText(GetSettingsFilename(), json);
        }
        catch (Exception ex)
        {
            App.CurrentApp.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                MessageBox.Show(ex.Message, "Error Saving Settings File", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }
    }
    
    public void LoadingFinished()
    {
        isLoading = false;
    }

    public static AppSettings Load()
    {
        var filename = GetSettingsFilename();
        var blankAppSettings = new AppSettings(false);
        if (!File.Exists(filename))
        {
            return blankAppSettings;
        }

        try
        {
            var json = File.ReadAllText(filename);
            var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                IncludeFields = false,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
                IgnoreReadOnlyFields = true,
                PropertyNameCaseInsensitive = true,
            };

            var newSettings = JsonSerializer.Deserialize<AppSettings>(json, options) ?? blankAppSettings;
            newSettings.LoadingFinished();
            return newSettings;
        }
        catch (IOException ex)
        {
            App.CurrentApp.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                MessageBox.Show(ex.Message, "Error Reading Settings File", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
            return blankAppSettings;
        }
        catch (Exception)

        {
            // Ignored
            return blankAppSettings;
        }
    }
}