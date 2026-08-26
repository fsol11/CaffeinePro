using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CaffeinePro.Classes;
using CaffeinePro.Services;

namespace CaffeinePro.Windows;

/// <summary>
/// Full-screen, blurred-backdrop warning, with a one-minute cancellable countdown, shown before the
/// configured afterwards action (e.g. Shutdown, Sleep, Lock) runs once an awakeness period ends.
/// One instance is shown per monitor so the whole desktop is covered, but only the instance on the
/// main display carries the countdown and the buttons - the rest are backdrop only. See
/// <see cref="OpenIt"/>.
/// </summary>
public partial class AfterwardsActionWarningWindow : INotifyPropertyChanged
{
    private const int CountdownSeconds = 60;
    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(350));

    // One window per monitor, all opened and closed together.
    private static readonly List<AfterwardsActionWarningWindow> _windows = [];

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly PixelRect _screenBounds;
    private readonly bool _isMainScreen;
    private IntPtr _hwnd;
    private int _secondsRemaining = CountdownSeconds;
    private bool _closingAnimated;

    public static void CloseIt()
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            foreach (var window in _windows.ToArray())
            {
                window._timer.Stop();

                if (window.IsLoaded)
                {
                    window.Close();
                }
            }

            _windows.Clear();
        });
    }

    /// <summary>
    /// Warns the user that <paramref name="action"/> is about to run and gives them
    /// <see cref="CountdownSeconds"/> seconds to cancel it or push it back. If not cancelled or
    /// snoozed in time, <paramref name="action"/> is executed automatically. If a warning is
    /// already open, it is closed and replaced with the new one.
    /// </summary>
    /// <remarks>
    /// Every monitor gets its own window covering exactly that monitor, rather than one window
    /// stretched across the virtual desktop: that keeps the countdown and the buttons centered on
    /// the main display instead of straddling the seam between two screens, and it copes with
    /// monitor layouts whose bounding rectangle contains gaps no display actually occupies.
    /// </remarks>
    public static void OpenIt(SessionAction action)
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            if (_windows.Count > 0)
            {
                CloseIt();
            }

            var virtualScreen = ScreenInfo.VirtualScreenBounds;
            var snapshot = CaptureVirtualScreen(virtualScreen);
            var screens = ScreenInfo.GetAll();
            var mainScreen = ScreenInfo.GetMain(screens);

            // Show the secondary screens first so the main one ends up on top and takes focus.
            foreach (var screen in screens.OrderBy(s => ReferenceEquals(s, mainScreen)))
            {
                var window = new AfterwardsActionWarningWindow(
                    action,
                    screen.Bounds,
                    ReferenceEquals(screen, mainScreen),
                    CropToScreen(snapshot, virtualScreen, screen.Bounds));

                _windows.Add(window);
                window.Show();
            }
        });
    }

    /// <summary>
    /// Private constructor to enforce singleton pattern. Use OpenIt() to create and show the windows.
    /// </summary>
    private AfterwardsActionWarningWindow(SessionAction action, PixelRect screenBounds, bool isMainScreen, BitmapSource backgroundSnapshot)
    {
        Action = action;
        _screenBounds = screenBounds;
        _isMainScreen = isMainScreen;
        BackgroundSnapshot = backgroundSnapshot;

        InitializeComponent();

        // Only the main display shows the countdown and the buttons; the other monitors are covered
        // by the blurred backdrop alone.
        ContentPanel.Visibility = isMainScreen ? Visibility.Visible : Visibility.Collapsed;
        ShowActivated = isMainScreen;

        Opacity = 0;

        Loaded += (_, _) =>
        {
            // WPF re-applies its own idea of the window rect while showing, so the snap has to be
            // repeated once the window is up - see SnapToScreen.
            SnapToScreen();
            AnimateIn();

            if (isMainScreen)
            {
                TakeForeground();
            }
        };

        // IsCancel on the Cancel button should already route Escape to it, but that relies on
        // focus/command routing that a full-screen backdrop window doesn't always have - this
        // guarantees Escape always cancels regardless, on whichever monitor's window has focus.
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            e.Handled = true;
            Cancel_Click(this, new RoutedEventArgs());
        };

        if (!isMainScreen)
        {
            return;
        }

        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    // Corner preference and titlebar-background removal are handled internally by the ui:FluentWindow
    // base class via the WindowCornerPreference / ExtendsContentIntoTitleBar properties set in XAML.

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;

        // Only the main display's window belongs in the taskbar; the backdrop-only ones are hidden
        // from it here rather than with ShowInTaskbar="False" - see WindowPlacement.HideFromTaskbar
        // for why that WPF property is avoided. Both this and the snap have to happen before the
        // first paint.
        if (!_isMainScreen)
        {
            WindowPlacement.HideFromTaskbar(_hwnd);
        }

        SnapToScreen();
    }

    /// <summary>
    /// Sizes the window to cover exactly the monitor it belongs to.
    /// </summary>
    private void SnapToScreen() => WindowPlacement.CoverScreen(_hwnd, _screenBounds);

    /// <summary>
    /// Pulls the window to the foreground and puts the keyboard focus on Cancel, so Escape - the
    /// way out of a full-screen warning - works without the user having to click the window first,
    /// and so the safest choice is the one Enter/Space acts on.
    /// </summary>
    private void TakeForeground()
    {
        WindowPlacement.TakeForeground(_hwnd);

        Activate();
        CancelButton.Focus();
        Keyboard.Focus(CancelButton);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Repeated once the window is actually on screen: while it is still being shown, Windows can
        // hand the foreground straight back to whatever had it.
        if (_isMainScreen)
        {
            TakeForeground();
        }
    }

    /// <summary>
    /// The part of the one-shot desktop screenshot that belongs to this window's monitor, used as
    /// the (blurred) window background. See <see cref="CaptureVirtualScreen"/> for why this is a
    /// screenshot rather than an OS window backdrop.
    /// </summary>
    public BitmapSource BackgroundSnapshot
    {
        get;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiObj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    private const int SrcCopy = 0x00CC0020;

    /// <summary>
    /// Takes a single screenshot of every monitor, right before the warning windows are shown.
    /// Windows' own Mica/Acrylic backdrop only ever blurs the desktop wallpaper, never other windows
    /// on screen (that's a platform limitation, not a bug here), so a real "see the blurred desktop
    /// behind this" look is done by hand: grab the desktop before these windows exist to cover it,
    /// then blur that bitmap as the windows' own background.
    /// </summary>
    private static BitmapSource CaptureVirtualScreen(PixelRect virtualScreen)
    {
        var screenDc = GetDC(IntPtr.Zero);
        var memDc = CreateCompatibleDC(screenDc);
        var bitmap = CreateCompatibleBitmap(screenDc, virtualScreen.Width, virtualScreen.Height);
        var oldBitmap = SelectObject(memDc, bitmap);

        BitBlt(memDc, 0, 0, virtualScreen.Width, virtualScreen.Height, screenDc, virtualScreen.Left, virtualScreen.Top, SrcCopy);

        SelectObject(memDc, oldBitmap);
        DeleteDC(memDc);
        ReleaseDC(IntPtr.Zero, screenDc);

        var snapshot = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        snapshot.Freeze();
        DeleteObject(bitmap);

        return snapshot;
    }

    /// <summary>
    /// Cuts one monitor's region out of the virtual-desktop screenshot, so each window shows the
    /// piece of desktop it is actually covering rather than a stretched copy of all of them.
    /// </summary>
    private static BitmapSource CropToScreen(BitmapSource snapshot, PixelRect virtualScreen, PixelRect screen)
    {
        var crop = new Int32Rect(
            screen.Left - virtualScreen.Left,
            screen.Top - virtualScreen.Top,
            screen.Width,
            screen.Height);

        // Guard against a display layout that changed between the capture and now.
        if (crop.X < 0 || crop.Y < 0 || crop.Width <= 0 || crop.Height <= 0 ||
            crop.X + crop.Width > snapshot.PixelWidth || crop.Y + crop.Height > snapshot.PixelHeight)
        {
            return snapshot;
        }

        var cropped = new CroppedBitmap(snapshot, crop);
        cropped.Freeze();

        return cropped;
    }

    private void AnimateIn()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, FadeDuration) { EasingFunction = ease });
    }

    private void AnimateOutAndClose()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        var fade = new DoubleAnimation(0, FadeDuration) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            _closingAnimated = true;
            Close();
        };

        BeginAnimation(OpacityProperty, fade);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Stop the countdown as soon as closing starts (e.g. via Escape) so the action can't fire
        // while the fade-out close animation is still playing.
        _timer.Stop();

        if (!_closingAnimated)
        {
            e.Cancel = true;
            AnimateOutAndClose();
            return;
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// The afterwards action that will run once the countdown reaches zero, unless cancelled or
    /// snoozed.
    /// </summary>
    public SessionAction Action
    {
        get;
    }

    public string ActionDescription => Routines.GetEnumDescription(Action);

    public int SecondsRemaining
    {
        get => _secondsRemaining;
        private set
        {
            SetField(ref _secondsRemaining, value);
            OnPropertyChanged(nameof(Progress));
        }
    }

    /// <summary>
    /// Fraction of the countdown remaining, from 1 (just opened) down to 0 (about to fire), for the
    /// progress bar.
    /// </summary>
    public double Progress => (double)SecondsRemaining / CountdownSeconds;

    private void Timer_Tick(object? sender, EventArgs e)
    {
        SecondsRemaining--;

        if (SecondsRemaining > 0)
        {
            return;
        }

        _timer.Stop();
        WindowsSessionService.ExecuteSessionAction(Action);
        CloseIt();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        CloseIt();
    }

    /// <summary>
    /// Handles the "keep awake for N more minutes" buttons: re-activates the keep-awake service for
    /// the requested duration, which also cancels the pending afterwards action.
    /// </summary>
    private void AddTime_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tagText } || !int.TryParse(tagText, out var minutes))
        {
            return;
        }

        _timer.Stop();
        App.CurrentApp.KeepAwakeService.Activate(new Awakeness(Awakeness.AwakenessTypes.Relative, TimeSpan.FromMinutes(minutes)));
        CloseIt();
    }

    // INotifyPropertyChanged implementation ---------------------------------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}
