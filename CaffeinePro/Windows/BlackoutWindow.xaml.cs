using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using CaffeinePro.Classes;

namespace CaffeinePro.Windows;

/// <summary>
/// Blacks out every monitor, so the machine looks switched off while it keeps running. Raised by the
/// blackout shortcut (see <see cref="Services.HotKeyService"/>) and dismissed with Escape.
/// </summary>
/// <remarks>
/// One window per monitor rather than a single window stretched over the virtual desktop: monitor
/// layouts are not necessarily one solid rectangle, and only a per-monitor window can cover each
/// screen exactly.
/// </remarks>
public partial class BlackoutWindow
{
    // One window per monitor, all opened and closed together.
    private static readonly List<BlackoutWindow> _windows = [];

    private readonly PixelRect _screenBounds;
    private readonly bool _isMainScreen;
    private IntPtr _hwnd;

    /// <summary>
    /// Shows the blackout, or dismisses it if it is already up - so the shortcut that raised it
    /// also takes it away, exactly like Escape does.
    /// </summary>
    public static void ToggleIt()
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            if (_windows.Count > 0)
            {
                CloseIt();
                return;
            }

            var screens = ScreenInfo.GetAll();
            var mainScreen = ScreenInfo.GetMain(screens);

            // Show the secondary screens first so the main one ends up on top and takes the focus.
            foreach (var screen in screens.OrderBy(s => ReferenceEquals(s, mainScreen)))
            {
                var window = new BlackoutWindow(screen.Bounds, ReferenceEquals(screen, mainScreen));
                _windows.Add(window);
                window.Show();
            }
        });
    }

    public static void CloseIt()
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            foreach (var window in _windows.ToArray())
            {
                if (window.IsLoaded)
                {
                    window.Close();
                }
            }

            _windows.Clear();
        });
    }

    /// <summary>
    /// Private constructor to keep the per-monitor set consistent. Use <see cref="ToggleIt"/>.
    /// </summary>
    private BlackoutWindow(PixelRect screenBounds, bool isMainScreen)
    {
        _screenBounds = screenBounds;
        _isMainScreen = isMainScreen;

        InitializeComponent();

        ShowActivated = isMainScreen;

        Loaded += (_, _) =>
        {
            WindowPlacement.CoverScreen(_hwnd, _screenBounds);

            if (!isMainScreen)
            {
                return;
            }

            TakeForeground();
            ShowAndFadeHint();
        };

        // Escape is the only way out, so it is handled on the window itself (in preview, ahead of
        // anything else) rather than relying on focus having landed on a particular control.
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            e.Handled = true;
            CloseIt();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;

        // Both have to happen before the first paint: the window must never flash at the wrong size
        // on its way up, and the taskbar style only takes effect if it is set before the window is
        // first shown.
        WindowPlacement.HideFromTaskbar(_hwnd);
        WindowPlacement.CoverScreen(_hwnd, _screenBounds);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Repeated once the window is actually on screen: while it is still being shown, Windows
        // can hand the foreground straight back to whatever had it.
        if (_isMainScreen)
        {
            TakeForeground();
        }
    }

    private void TakeForeground()
    {
        WindowPlacement.TakeForeground(_hwnd);
        Activate();
        Focus();
        Keyboard.Focus(this);
    }

    /// <summary>
    /// Shows the "Press Esc to return" hint just long enough to be read, then fades it out so the
    /// screen ends up completely black.
    /// </summary>
    private void ShowAndFadeHint()
    {
        Hint.Visibility = Visibility.Visible;

        var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(800)))
        {
            BeginTime = TimeSpan.FromSeconds(2.5),
        };

        Hint.BeginAnimation(OpacityProperty, fade);
    }
}
