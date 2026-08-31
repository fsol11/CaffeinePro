// -----------------------------------------------------------------------
// <copyright file="Dialogs.cs" company="Lotrasoft Inc.">
//     Copyright (c) 2026 Lotrasoft Inc. All rights reserved.
// </copyright>
// <author>Farshid Solimanpour</author>
// -----------------------------------------------------------------------
// This file is part of the Caffeine Pro project.
// The Caffeine Pro project is licensed under MIT License.
// For more details, see the LICENSE file in the project root.
// -----------------------------------------------------------------------

using System.Windows;
using CaffeinePro.Localization;

namespace CaffeinePro.Classes;

/// <summary>
/// The application's message boxes.
/// </summary>
/// <remarks>
/// Wrapped rather than calling <see cref="MessageBox"/> directly because a plain message box is
/// the one piece of UI that does not inherit the app's reading direction: it is drawn by Windows,
/// which has to be told through <see cref="MessageBoxOptions"/>. Without this, dialogs in Arabic
/// and Farsi would be left-aligned with their buttons on the wrong side.
/// </remarks>
public static class Dialogs
{
    public static MessageBoxResult Show(
        string text,
        string caption,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None)
    {
        var options = LocalizationService.Instance.EffectiveLanguage.IsRightToLeft
            ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
            : MessageBoxOptions.None;

        return MessageBox.Show(text, caption, button, icon, MessageBoxResult.None, options);
    }

    /// <summary>
    /// Shows an error, titled with the shared "Error" caption.
    /// </summary>
    public static void ShowError(string text) =>
        Show(text, LocalizationService.Get("Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
}
