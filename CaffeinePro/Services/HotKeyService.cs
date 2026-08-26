using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using CaffeinePro.Classes;

namespace CaffeinePro.Services;

/// <summary>
/// Owns the application's system-wide keyboard shortcuts. Registers the blackout-screen shortcut
/// with Windows, keeps it in step with the setting, and raises <see cref="BlackoutRequested"/> when
/// it is pressed - from anywhere, whichever application has focus.
/// </summary>
public sealed class HotKeyService : IDisposable, INotifyPropertyChanged
{
    private const int BlackoutHotKeyId = 1;
    private const int WM_HOTKEY = 0x0312;

    private readonly AppSettings _appSettings;
    private HwndSource? _messageWindow;
    private bool _isBlackoutHotKeyRegistered;
    private bool _isDisposed;

    public HotKeyService(AppSettings appSettings)
    {
        _appSettings = appSettings;
        _appSettings.PropertyChanged += OnAppSettingsChanged;
    }

    /// <summary>
    /// Raised on the UI thread when the blackout shortcut is pressed.
    /// </summary>
    public event EventHandler? BlackoutRequested;

    /// <summary>
    /// False when Windows refused the shortcut - almost always because another application already
    /// owns that combination. The settings UI shows this so the user knows to pick another one.
    /// </summary>
    public bool IsBlackoutHotKeyRegistered
    {
        get => _isBlackoutHotKeyRegistered;
        private set
        {
            if (_isBlackoutHotKeyRegistered == value)
            {
                return;
            }

            _isBlackoutHotKeyRegistered = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Creates the hidden message window that receives WM_HOTKEY and registers the current
    /// shortcut. Call once, from the UI thread, after the application has started.
    /// </summary>
    public void Start()
    {
        // A message-only window: never shown, never in the taskbar, exists purely as the target
        // Windows delivers WM_HOTKEY to. The application is a tray app with no main window, so
        // there is no existing HWND to hang the hotkey off.
        _messageWindow = new HwndSource(new HwndSourceParameters(nameof(HotKeyService))
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ParentWindow = HWND_MESSAGE,
        });

        _messageWindow.AddHook(OnMessage);

        RegisterBlackoutHotKey();
    }

    /// <summary>
    /// Releases the shortcut while the user is recording a replacement, so the combination they
    /// press reaches the recorder instead of being swallowed by the registration it is replacing.
    /// Pair with <see cref="Resume"/>.
    /// </summary>
    public void Suspend()
    {
        UnregisterBlackoutHotKey();
    }

    /// <summary>
    /// Re-registers the shortcut after <see cref="Suspend"/>.
    /// </summary>
    public void Resume()
    {
        RegisterBlackoutHotKey();
    }

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.BlackoutHotKey))
        {
            RegisterBlackoutHotKey();
        }
    }

    private void RegisterBlackoutHotKey()
    {
        if (_messageWindow is null || _isDisposed)
        {
            return;
        }

        UnregisterBlackoutHotKey();

        var hotKey = _appSettings.BlackoutHotKey;
        if (!hotKey.IsValid)
        {
            return;
        }

        // MOD_NOREPEAT: holding the shortcut down should black the screen out once, not repeatedly.
        var modifiers = ToNativeModifiers(hotKey.Modifiers) | MOD_NOREPEAT;
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key);

        IsBlackoutHotKeyRegistered = RegisterHotKey(_messageWindow.Handle, BlackoutHotKeyId, modifiers, virtualKey);
    }

    private void UnregisterBlackoutHotKey()
    {
        if (_messageWindow is null || !IsBlackoutHotKeyRegistered)
        {
            return;
        }

        UnregisterHotKey(_messageWindow.Handle, BlackoutHotKeyId);
        IsBlackoutHotKeyRegistered = false;
    }

    private IntPtr OnMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY || wParam.ToInt32() != BlackoutHotKeyId)
        {
            return IntPtr.Zero;
        }

        handled = true;
        BlackoutRequested?.Invoke(this, EventArgs.Empty);

        return IntPtr.Zero;
    }

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        var native = 0u;

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            native |= MOD_ALT;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            native |= MOD_CONTROL;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            native |= MOD_SHIFT;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            native |= MOD_WIN;
        }

        return native;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _appSettings.PropertyChanged -= OnAppSettingsChanged;

        UnregisterBlackoutHotKey();

        _messageWindow?.RemoveHook(OnMessage);
        _messageWindow?.Dispose();
        _messageWindow = null;
    }

    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
