using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CaffeinePro.Classes;

namespace CaffeinePro.Windows;

/// <summary>
/// Small modal dialog that records a new keyboard shortcut: it listens for the next key combination
/// the user presses and offers it for saving.
/// </summary>
/// <remarks>
/// A dialog rather than an inline editor in the tray menu, because recording needs real keyboard
/// focus and a WPF context menu handles keystrokes itself (menu navigation, access keys).
/// </remarks>
public partial class HotKeyRecorderWindow
{
    private HotKey _recorded;
    private IntPtr _hwnd;
    private HotKeyRecorderHook? _hook;

    private HotKeyRecorderWindow(HotKey current)
    {
        InitializeComponent();

        _recorded = current;
        UpdatePreview();

        // The hook is what actually reads the keystrokes (see HotKeyRecorderHook for why WPF's own
        // key events are not enough); it lives exactly as long as the dialog.
        Loaded += (_, _) => _hook = new HotKeyRecorderHook(OnCombination);
        Closed += (_, _) =>
        {
            _hook?.Dispose();
            _hook = null;
        };
    }

    /// <summary>
    /// Called by the hook for each keystroke, on the thread the hook runs on - the UI thread, since
    /// that is where the dialog and its message pump live.
    /// </summary>
    private void OnCombination(HotKey combination)
    {
        // Backspace on its own clears the shortcut; with modifiers it is a shortcut key like any
        // other, so it is only treated as "clear" when pressed alone.
        _recorded = combination is { Modifiers: ModifierKeys.None, Key: Key.Back or Key.Delete }
            ? HotKey.None
            : combination;

        UpdatePreview();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Opened from the tray menu while another application owns the foreground, so Windows will
        // not simply hand the focus over - and a dialog that records keystrokes is useless without
        // it. Same treatment as the full-screen overlays.
        WindowPlacement.TakeForeground(_hwnd);
        Activate();
        Keyboard.Focus(this);
    }

    /// <summary>
    /// Asks the user for a new shortcut. Returns the chosen combination, or null if they cancelled.
    /// </summary>
    /// <remarks>
    /// The shortcut being replaced is released for the duration: while it is registered with
    /// Windows it never reaches this window, so pressing it would record nothing.
    /// </remarks>
    public static HotKey? AskForHotKey(HotKey current, Window? owner = null)
    {
        var hotKeyService = App.CurrentApp.HotKeyService;
        hotKeyService.Suspend();

        try
        {
            var window = new HotKeyRecorderWindow(current);

            if (owner is not null && !ReferenceEquals(owner, window))
            {
                window.Owner = owner;
            }

            return window.ShowDialog() == true ? window._recorded : null;
        }
        finally
        {
            hotKeyService.Resume();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        // Everything else is recorded by the hook and never gets this far; Escape is deliberately
        // let through so the dialog can still be cancelled, and Backspace clears the shortcut.
        switch (e.Key)
        {
            case Key.Escape:
                e.Handled = true;
                DialogResult = false;
                return;

            case Key.Back or Key.Delete:
                e.Handled = true;
                _recorded = HotKey.None;
                UpdatePreview();
                return;
        }
    }

    private void UpdatePreview()
    {
        var modifiersOnly = !_recorded.IsSet && _recorded.Modifiers != ModifierKeys.None;

        PreviewText.Text = modifiersOnly
            ? _recorded.DisplayModifiers() + "..."
            : _recorded.DisplayText;

        // "None" (no shortcut) is a legitimate choice - it just switches the feature off.
        SaveButton.IsEnabled = _recorded.IsValid || _recorded == HotKey.None;

        StatusText.Text = _recorded switch
        {
            { IsValid: true } => "Press Save to use this shortcut.",
            { IsSet: true } => "This combination needs at least one modifier (Ctrl, Alt, Shift or Win).",
            _ when _recorded.Modifiers != ModifierKeys.None => "Now press the key to go with these modifiers.",
            _ => "No shortcut: the blackout screen can then only be opened from the menu.",
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
