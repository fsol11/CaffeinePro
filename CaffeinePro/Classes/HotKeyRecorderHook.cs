using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace CaffeinePro.Classes;

/// <summary>
/// Captures raw keystrokes while the user is recording a new shortcut, and swallows them so nothing
/// else acts on them.
/// </summary>
/// <remarks>
/// A low-level keyboard hook rather than ordinary WPF key events, because the combinations most
/// worth recording are exactly the ones Windows keeps for itself: Win+/ (IME reconversion), Win+D,
/// Win+B and so on never reach a window's key handlers at all. A hook sits ahead of that, and
/// swallowing what it sees means recording Win+D does not also minimise every window on the way
/// past. Escape is the one key let through, so the dialog can still be cancelled.
/// </remarks>
public sealed class HotKeyRecorderHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int HC_ACTION = 0;

    private readonly Action<HotKey> _onCombination;
    private readonly LowLevelKeyboardProc _callback;

    // Which keys are currently held. Tracked here rather than read back from Windows, because the
    // hook swallows the very key presses Windows would otherwise have recorded the state from.
    private readonly HashSet<int> _keysDown = [];

    private IntPtr _hookId;

    /// <summary>
    /// Installs the hook. <paramref name="onCombination"/> is called for every key press with the
    /// combination as it currently stands - a modifier-only <see cref="HotKey"/> while the user is
    /// still holding keys down, and a complete one once they press a real key.
    /// </summary>
    public HotKeyRecorderHook(Action<HotKey> onCombination)
    {
        _onCombination = onCombination;
        _callback = HookCallback;

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;

        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(module?.ModuleName), 0);
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code != HC_ACTION)
        {
            return CallNextHookEx(_hookId, code, wParam, lParam);
        }

        var message = wParam.ToInt32();
        var isDown = message is WM_KEYDOWN or WM_SYSKEYDOWN;
        var isUp = message is WM_KEYUP or WM_SYSKEYUP;

        if (!isDown && !isUp)
        {
            return CallNextHookEx(_hookId, code, wParam, lParam);
        }

        var virtualKey = Marshal.ReadInt32(lParam);
        var key = KeyInterop.KeyFromVirtualKey(virtualKey);

        // Escape stays a normal keystroke: it is how the dialog is cancelled.
        if (key == Key.Escape)
        {
            return CallNextHookEx(_hookId, code, wParam, lParam);
        }

        if (isDown)
        {
            _keysDown.Add(virtualKey);

            var modifiers = GetModifiers();
            _onCombination(HotKey.IsModifierKey(key)
                ? new HotKey(modifiers, Key.None)
                : new HotKey(modifiers, key));
        }
        else if (!_keysDown.Remove(virtualKey))
        {
            // A key that went down before recording started: let its release through so whichever
            // application is holding it does not end up with a key stuck down.
            return CallNextHookEx(_hookId, code, wParam, lParam);
        }

        // Swallow it: while recording, keystrokes are input for the recorder, not for Windows.
        return 1;
    }

    /// <summary>
    /// The modifiers among the currently held keys.
    /// </summary>
    private ModifierKeys GetModifiers()
    {
        var modifiers = ModifierKeys.None;

        foreach (var virtualKey in _keysDown)
        {
            modifiers |= KeyInterop.KeyFromVirtualKey(virtualKey) switch
            {
                Key.LeftCtrl or Key.RightCtrl => ModifierKeys.Control,
                Key.LeftAlt or Key.RightAlt => ModifierKeys.Alt,
                Key.LeftShift or Key.RightShift => ModifierKeys.Shift,
                Key.LWin or Key.RWin => ModifierKeys.Windows,
                _ => ModifierKeys.None,
            };
        }

        return modifiers;
    }

    public void Dispose()
    {
        if (_hookId == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookType, LowLevelKeyboardProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hookId);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hookId, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
