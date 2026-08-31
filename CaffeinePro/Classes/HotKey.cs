using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using CaffeinePro.Localization;

namespace CaffeinePro.Classes;

/// <summary>
/// A system-wide keyboard shortcut: zero or more modifiers plus one key.
/// </summary>
/// <remarks>
/// Stored in the settings file as a round-trippable "Modifier+Modifier+Key" string of enum names
/// (see <see cref="ToString"/>), and shown to the user in the friendlier
/// <see cref="DisplayText"/> form - "Win + /" rather than "Windows+OemQuestion".
/// </remarks>
[JsonConverter(typeof(HotKeyJsonConverter))]
public readonly record struct HotKey(ModifierKeys Modifiers, Key Key)
{
    /// <summary>
    /// No shortcut assigned - the feature it drives is switched off.
    /// </summary>
    public static readonly HotKey None = new(ModifierKeys.None, Key.None);

    /// <summary>
    /// The out-of-the-box shortcut for the blackout screen: Windows + /.
    /// </summary>
    public static readonly HotKey DefaultBlackout = new(ModifierKeys.Windows, Key.OemQuestion);

    /// <summary>
    /// True when an actual key (not just modifiers) is assigned.
    /// </summary>
    [JsonIgnore]
    public bool IsSet => Key != Key.None;

    /// <summary>
    /// Whether this combination is one Windows will accept as a global hotkey. Windows needs at
    /// least one modifier, otherwise the shortcut would swallow a plain keystroke from every other
    /// application.
    /// </summary>
    [JsonIgnore]
    public bool IsValid => IsSet && Modifiers != ModifierKeys.None && !IsModifierKey(Key);

    /// <summary>
    /// The combination as shown to the user, e.g. "Win + /".
    /// </summary>
    [JsonIgnore]
    public string DisplayText =>
        IsSet ? DisplayModifiers() + GetKeyText(Key) : LocalizationService.Get("HotKey_None");

    /// <summary>
    /// Just the modifier part of <see cref="DisplayText"/>, each one followed by " + ", so a
    /// half-entered combination can be shown while the user is still holding keys down.
    /// </summary>
    public string DisplayModifiers()
    {
        var text = new StringBuilder();

        // Translated, because Windows itself labels these keys in the user's language - a German
        // keyboard has "Strg" on it, not "Ctrl".
        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            text.Append(LocalizationService.Get("HotKey_Ctrl")).Append(" + ");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            text.Append(LocalizationService.Get("HotKey_Alt")).Append(" + ");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            text.Append(LocalizationService.Get("HotKey_Shift")).Append(" + ");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            text.Append(LocalizationService.Get("HotKey_Win")).Append(" + ");
        }

        return text.ToString();
    }

    /// <summary>
    /// True for the modifier keys themselves, which can never be the shortcut's main key.
    /// </summary>
    public static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin
            or Key.System;

    /// <summary>
    /// The storage form: enum names joined with '+', e.g. "Windows+OemQuestion".
    /// </summary>
    public override string ToString()
    {
        if (!IsSet)
        {
            return nameof(None);
        }

        var parts = new List<string>();

        foreach (var modifier in new[] { ModifierKeys.Control, ModifierKeys.Alt, ModifierKeys.Shift, ModifierKeys.Windows })
        {
            if (Modifiers.HasFlag(modifier))
            {
                parts.Add(modifier.ToString());
            }
        }

        parts.Add(Key.ToString());

        return string.Join('+', parts);
    }

    /// <summary>
    /// Reads back what <see cref="ToString"/> wrote, falling back to <paramref name="fallback"/> for
    /// anything unrecognized so a hand-edited or older settings file can never break startup.
    /// </summary>
    public static HotKey Parse(string? text, HotKey fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        if (string.Equals(text.Trim(), nameof(None), StringComparison.OrdinalIgnoreCase))
        {
            return None;
        }

        var modifiers = ModifierKeys.None;
        var key = Key.None;

        foreach (var part in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<ModifierKeys>(part, true, out var modifier) && modifier != ModifierKeys.None)
            {
                modifiers |= modifier;
            }
            else if (Enum.TryParse<Key>(part, true, out var parsedKey))
            {
                key = parsedKey;
            }
            else
            {
                return fallback;
            }
        }

        return key == Key.None ? fallback : new HotKey(modifiers, key);
    }

    /// <summary>
    /// The printable name of a key: the character it types where there is one, so the user sees
    /// "/" instead of "OemQuestion".
    /// </summary>
    private static string GetKeyText(Key key) => key switch
    {
        Key.OemQuestion => "/",
        Key.OemPeriod => ".",
        Key.OemComma => ",",
        Key.OemMinus => "-",
        Key.OemPlus => "=",
        Key.OemTilde => "`",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemPipe => "\\",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemBackslash => "\\",
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 =>
            LocalizationService.Format("HotKey_NumPadFormat", key - Key.NumPad0),
        _ => key.ToString(),
    };
}

/// <summary>
/// Persists a <see cref="HotKey"/> as its string form, so the settings file stays readable and
/// survives changes to the underlying enums.
/// </summary>
public sealed class HotKeyJsonConverter : JsonConverter<HotKey>
{
    public override HotKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => HotKey.Parse(reader.GetString(), HotKey.None);

    public override void Write(Utf8JsonWriter writer, HotKey value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
