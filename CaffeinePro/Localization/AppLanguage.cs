// -----------------------------------------------------------------------
// <copyright file="AppLanguage.cs" company="Lotrasoft Inc.">
//     Copyright (c) 2026 Lotrasoft Inc. All rights reserved.
// </copyright>
// <author>Farshid Solimanpour</author>
// -----------------------------------------------------------------------
// This file is part of the Caffeine Pro project.
// The Caffeine Pro project is licensed under MIT License.
// For more details, see the LICENSE file in the project root.
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CaffeinePro.Localization;

/// <summary>
/// One entry of the language menu: either a language the UI ships in, or the "follow Windows"
/// entry that stands in for whichever of them matches the display language.
/// </summary>
/// <remarks>
/// Implements <see cref="INotifyPropertyChanged"/> because the menu is bound directly to these
/// instances: both the check mark and - for the system entry - the label itself change when the
/// language is switched, and the menu has to follow without being rebuilt.
/// </remarks>
public sealed class AppLanguage : INotifyPropertyChanged
{
    private readonly string _nativeName;
    private bool _isSelected;

    /// <param name="code">
    /// The culture name used for resource lookup and for formatting dates and numbers. Empty for
    /// the "follow Windows" entry, which has no culture of its own.
    /// </param>
    /// <param name="nativeName">The language's own name for itself, as shown in the menu.</param>
    /// <param name="isRightToLeft">True for right-to-left scripts (Arabic, Farsi).</param>
    /// <param name="usesNativeDigits">
    /// True for languages that write numbers in their own digits rather than 0-9. Deliberately a
    /// separate flag from <paramref name="isRightToLeft"/>: the two coincide for the languages
    /// shipped today, but they are different questions about a script.
    /// </param>
    /// <param name="fontStack">
    /// Comma separated WPF font fallback list covering the language's script. The Latin languages
    /// deliberately share the same stack, so switching between them changes nothing visually.
    /// </param>
    internal AppLanguage(string code, string nativeName, bool isRightToLeft, bool usesNativeDigits,
        string fontStack)
    {
        Code = code;
        _nativeName = nativeName;
        IsRightToLeft = isRightToLeft;
        UsesNativeDigits = usesNativeDigits;
        FontStack = fontStack;
    }

    /// <summary>
    /// The culture name, or an empty string for the "follow Windows" entry.
    /// </summary>
    public string Code
    {
        get;
    }

    /// <summary>
    /// True for the entry that follows the Windows display language rather than naming one.
    /// </summary>
    public bool IsSystemDefault => Code.Length == 0;

    /// <summary>
    /// What the language menu shows. Every real language names itself in its own script, so it
    /// stays readable whichever language the rest of the UI is in; only the "follow Windows" entry
    /// is translated along with everything else.
    /// </summary>
    public string DisplayName =>
        IsSystemDefault ? LocalizationService.Get("Language_SystemDefault") : _nativeName;

    /// <summary>
    /// True for right-to-left scripts. See <see cref="LocalizationService.FlowDirection"/>.
    /// </summary>
    public bool IsRightToLeft
    {
        get;
    }

    /// <summary>
    /// True when numbers belong in the language's own digits (Persian ۱۲۳, Arabic ١٢٣) rather than
    /// 0-9. See <see cref="LocalizationService.DigitSubstitution"/>.
    /// </summary>
    public bool UsesNativeDigits
    {
        get;
    }

    /// <summary>
    /// The font fallback list for this language's script. See <see cref="LocalizationService.FontFamily"/>.
    /// </summary>
    public string FontStack
    {
        get;
    }

    /// <summary>
    /// True while this is the entry the user picked - which is the "follow Windows" entry, not the
    /// language it resolves to, when that is what was chosen.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        internal set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Re-reads <see cref="DisplayName"/>, which is translated for the "follow Windows" entry.
    /// </summary>
    internal void RefreshDisplayName() => OnPropertyChanged(nameof(DisplayName));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
