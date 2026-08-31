// -----------------------------------------------------------------------
// <copyright file="LocalizationService.cs" company="Lotrasoft Inc.">
//     Copyright (c) 2026 Lotrasoft Inc. All rights reserved.
// </copyright>
// <author>Farshid Solimanpour</author>
// -----------------------------------------------------------------------
// This file is part of the Caffeine Pro project.
// The Caffeine Pro project is licensed under MIT License.
// For more details, see the LICENSE file in the project root.
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace CaffeinePro.Localization;

/// <summary>
/// The single place the UI reads translated text, reading direction and script font from.
/// </summary>
/// <remarks>
/// Everything is exposed as bindable state on one long-lived instance rather than resolved once at
/// load time, so switching language re-renders the whole UI in place: <see cref="LocExtension"/>
/// binds each piece of text to <see cref="this[string]"/>, and window roots bind their
/// <see cref="FlowDirection"/> and <see cref="FontFamily"/> to the properties below. Text that is
/// cached in C# rather than bound (the tray tooltip, the awakeness texts) is refreshed from
/// <see cref="LanguageChanged"/> instead.
/// </remarks>
public sealed class LocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// The <see cref="AppLanguage.Code"/> that means "use whatever Windows is set to".
    /// </summary>
    public const string SystemDefaultCode = "";

    /// <summary>
    /// The font stack shared by every Latin-script language. Kept identical across them so that
    /// switching between English, French, Spanish, German and Portuguese cannot alter typography.
    /// </summary>
    private const string LatinFonts = "Segoe UI";

    private static readonly ResourceManager Strings =
        new("CaffeinePro.Resources.Strings", typeof(App).Assembly);

    /// <summary>
    /// The Windows display language, captured before anything is applied - once
    /// <see cref="Apply"/> has run, the current UI culture is the app's choice and no longer says
    /// anything about the machine.
    /// </summary>
    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;

    public static LocalizationService Instance { get; } = new();

    private LocalizationService()
    {
        Languages =
        [
            new AppLanguage(SystemDefaultCode, string.Empty, false, false, LatinFonts),
            new AppLanguage("en", "English", false, false, LatinFonts),
            new AppLanguage("fr", "Français", false, false, LatinFonts),
            new AppLanguage("es", "Español", false, false, LatinFonts),
            new AppLanguage("de", "Deutsch", false, false, LatinFonts),
            new AppLanguage("pt", "Português", false, false, LatinFonts),
            new AppLanguage("ja", "日本語", false, false, "Yu Gothic UI, Meiryo UI, Segoe UI"),
            new AppLanguage("zh-Hans", "简体中文", false, false, "Microsoft YaHei UI, Microsoft YaHei, Segoe UI"),
            new AppLanguage("ar", "العربية", true, true, "Segoe UI, Tahoma, Arial"),
            new AppLanguage("fa", "فارسی", true, true, "Segoe UI, Tahoma, Arial"),
        ];

        // English is only a starting point so the properties are never null; App applies the
        // stored setting before the first window is shown.
        EffectiveLanguage = Languages[1];
        SelectedCode = SystemDefaultCode;
    }

    /// <summary>
    /// Every entry of the language menu, starting with "follow Windows".
    /// </summary>
    public IReadOnlyList<AppLanguage> Languages
    {
        get;
    }

    /// <summary>
    /// The language the UI is actually rendered in. Never the "follow Windows" entry: that one is
    /// resolved to a real language here.
    /// </summary>
    public AppLanguage EffectiveLanguage
    {
        get;
        private set;
    }

    /// <summary>
    /// The <see cref="AppLanguage.Code"/> the user picked, which is an empty string when they chose
    /// to follow Windows. This is what gets persisted.
    /// </summary>
    public string SelectedCode
    {
        get;
        private set;
    }

    /// <summary>
    /// The reading direction of the current language. Window and menu roots bind to this, so
    /// Arabic and Farsi mirror the entire layout.
    /// </summary>
    public FlowDirection FlowDirection =>
        EffectiveLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <summary>
    /// A font that covers the current language's script. Latin languages all resolve to the same
    /// family, so only Arabic, Farsi, Japanese and Chinese actually change the typeface.
    /// </summary>
    public FontFamily FontFamily => new(EffectiveLanguage.FontStack);

    /// <summary>
    /// Flips a directional icon horizontally in a right-to-left language, and leaves it alone
    /// otherwise.
    /// </summary>
    /// <remarks>
    /// <see cref="FlowDirection"/> mirrors layout, and it mirrors vector artwork with it - but not
    /// a glyph from an icon font, which is drawn as a character and comes out pointing the wrong
    /// way. The submenu chevron is exactly that, so it is turned round by hand. Bound rather than
    /// assigned, so it follows a language change like everything else.
    /// </remarks>
    public Transform MirrorTransform => EffectiveLanguage.IsRightToLeft ? FlipHorizontal : Transform.Identity;

    private static readonly Transform FlipHorizontal = CreateFlipHorizontal();

    private static Transform CreateFlipHorizontal()
    {
        var transform = new ScaleTransform(-1, 1);
        transform.Freeze(); // <- shared by every icon that uses it
        return transform;
    }

    /// <summary>
    /// Which digits numbers are drawn with. Bound by window and menu roots to the inherited
    /// <see cref="System.Windows.Media.NumberSubstitution"/> attached properties.
    /// </summary>
    /// <remarks>
    /// This has to be stated rather than left to WPF, for two reasons. WPF's own default takes the
    /// number culture from <c>xml:lang</c>, which is en-US whatever the app is set to, so nothing
    /// would ever be substituted. And Arabic and Farsi ask for <c>DigitShapes.Context</c>, which
    /// shapes a digit after the strong character in front of it - so a number at the start of a
    /// string stays Latin while the same number mid-sentence turns native, which reads as a bug.
    /// <c>NativeNational</c> settles it: every digit is drawn the same way.
    /// <para>
    /// Only the drawing changes. The underlying strings keep their 0-9, so the code that reads text
    /// back out of the UI - <see cref="Classes.Routines.ContentToTimeSpan"/> and the hour buttons -
    /// carries on parsing exactly as before.
    /// </para>
    /// </remarks>
    public NumberSubstitutionMethod DigitSubstitution =>
        EffectiveLanguage.UsesNativeDigits
            ? NumberSubstitutionMethod.NativeNational
            : NumberSubstitutionMethod.European;

    /// <summary>
    /// Looks a string up by key. Exposed as an indexer because that is what XAML can bind to -
    /// see <see cref="LocExtension"/>.
    /// </summary>
    public string this[string key] => Get(key);

    /// <summary>
    /// Raised after the language has been switched, for text that is built in C# and cached rather
    /// than bound through <see cref="LocExtension"/>.
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <summary>
    /// Returns the translation of <paramref name="key"/> in the current language.
    /// </summary>
    /// <returns>
    /// The translated text, or the key itself if it is missing - a visible but harmless
    /// placeholder, rather than an exception in front of the user. Debug builds assert instead,
    /// so a typo surfaces during development.
    /// </returns>
    public static string Get(string key)
    {
        var value = TryGet(key);

        if (value != null)
        {
            return value;
        }

        Debug.Fail($"Missing localized string: '{key}'");
        return key;
    }

    /// <summary>
    /// Looks a string up without treating its absence as a mistake.
    /// </summary>
    /// <returns>The translated text, or <c>null</c> when the key is not in the table.</returns>
    /// <remarks>
    /// For keys that are composed at run time and are genuinely allowed to be missing - see
    /// <see cref="Classes.Routines.GetEnumDescription"/>, where a key per enum member is optional.
    /// </remarks>
    public static string? TryGet(string key)
    {
        var value = Strings.GetString(key, CultureInfo.CurrentUICulture);
        return value == null ? null : ToNativeDigits(value);
    }

    /// <summary>
    /// Returns the translation of <paramref name="key"/> with <paramref name="args"/> substituted
    /// into its placeholders, formatted for the current language.
    /// </summary>
    /// <remarks>
    /// Formats from the raw text rather than from <see cref="Get"/>: the digits have to be
    /// converted after the substitution, both so that the numbers passed in are converted too, and
    /// because converting first would turn the "0" of a "{0}" placeholder into a native digit and
    /// leave <see cref="string.Format(IFormatProvider,string,object?[])"/> with nothing to fill in.
    /// </remarks>
    public static string Format(string key, params object?[] args)
    {
        var format = Strings.GetString(key, CultureInfo.CurrentUICulture);

        if (format == null)
        {
            Debug.Fail($"Missing localized string: '{key}'");
            format = key;
        }

        return ToNativeDigits(string.Format(CultureInfo.CurrentCulture, format, args));
    }

    /// <summary>
    /// Rewrites 0-9 into the current language's own digits, for languages that use them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WPF can draw digits in a native form on its own, and for text written directly in XAML it
    /// does. It cannot help with the text this app builds in C#, though: a number formatted into a
    /// string arrives as 0-9 and is drawn that way, which is how a Farsi UI ended up with Persian
    /// digits in some places and Latin ones in others.
    /// </para>
    /// <para>
    /// Only text on its way to the screen goes through here. Text that is also a value - the hour
    /// buttons' <see cref="Controls.HourItem.HourLabel"/> - keeps its 0-9 and is drawn in native
    /// digits by WPF instead, so nothing downstream has to cope with digits it cannot parse.
    /// </para>
    /// </remarks>
    public static string ToNativeDigits(string text)
    {
        var language = Instance.EffectiveLanguage;

        if (!language.UsesNativeDigits || text.Length == 0)
        {
            return text;
        }

        var digits = CultureInfo.CurrentCulture.NumberFormat.NativeDigits;
        char[]? converted = null;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is < '0' or > '9')
            {
                continue;
            }

            // Only pay for a copy once an actual digit turns up.
            converted ??= text.ToCharArray();
            converted[i] = digits[text[i] - '0'][0];
        }

        return converted == null ? text : new string(converted);
    }

    /// <summary>
    /// Switches the UI to the given language and refreshes everything bound to this service.
    /// </summary>
    /// <param name="code">
    /// An <see cref="AppLanguage.Code"/>, or <see cref="SystemDefaultCode"/> to follow Windows.
    /// An unknown code falls back to following Windows, so a hand-edited settings file cannot
    /// leave the app without a language.
    /// </param>
    public void Apply(string? code)
    {
        var selected = Languages.FirstOrDefault(language => language.Code == code) ?? Languages[0];
        var effective = selected.IsSystemDefault ? MatchSystemLanguage() : selected;

        var culture = new CultureInfo(effective.Code);

        // Both cultures are set: the UI culture picks the satellite assembly, and the formatting
        // culture is what dates, times and numbers in the same text are rendered with.
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        SelectedCode = selected.Code;
        EffectiveLanguage = effective;

        foreach (var language in Languages)
        {
            language.IsSelected = language.Code == selected.Code;
            language.RefreshDisplayName();
        }

        OnPropertyChanged(nameof(EffectiveLanguage));
        OnPropertyChanged(nameof(SelectedCode));
        OnPropertyChanged(nameof(FlowDirection));
        OnPropertyChanged(nameof(FontFamily));
        OnPropertyChanged(nameof(MirrorTransform));
        OnPropertyChanged(nameof(DigitSubstitution));

        // "Item[]" is what WPF listens for to re-evaluate every indexer binding, which is how one
        // notification refreshes all the text on screen at once.
        OnPropertyChanged("Item[]");

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Picks the shipped language closest to the Windows display language.
    /// </summary>
    /// <remarks>
    /// Matched on the two-letter code, so every regional variant lands on the right language
    /// (fr-CA on French, pt-BR on Portuguese). Chinese only ships as Simplified, so zh-Hant
    /// machines get that rather than English. Anything unrecognized falls back to English.
    /// </remarks>
    private AppLanguage MatchSystemLanguage()
    {
        var system = SystemUiCulture.TwoLetterISOLanguageName;

        return Languages.FirstOrDefault(language =>
                   !language.IsSystemDefault
                   && new CultureInfo(language.Code).TwoLetterISOLanguageName == system)
               ?? Languages[1];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
