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

namespace CaffeinePro;

/// <summary>
/// Full-screen, blurred-backdrop warning, with a one-minute cancellable countdown, shown before the
/// configured afterwards action (e.g. Shutdown, Sleep, Lock) runs once an awakeness period ends.
/// Spans every monitor so it cannot be missed regardless of which screen the user is looking at.
/// </summary>
public partial class AfterwardsActionWarningWindow : INotifyPropertyChanged
{
    private const int CountdownSeconds = 60;
    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(350));

    // Singleton instance to ensure only one warning window is open at a time
    private static AfterwardsActionWarningWindow? _window;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _secondsRemaining = CountdownSeconds;
    private bool _closingAnimated;

    public static void CloseIt()
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            if (_window is { IsLoaded: true })
            {
                _window.Close();
            }

            _window = null;
        });
    }

    /// <summary>
    /// Warns the user that <paramref name="action"/> is about to run and gives them
    /// <see cref="CountdownSeconds"/> seconds to cancel it or push it back. If not cancelled or
    /// snoozed in time, <paramref name="action"/> is executed automatically. If a warning is
    /// already open, it is closed and replaced with the new one.
    /// </summary>
    public static void OpenIt(SessionAction action)
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            if (_window is { IsLoaded: true })
            {
                CloseIt();
            }

            _window = new AfterwardsActionWarningWindow(action);
            _window.Show();
        });
    }

    /// <summary>
    /// Private constructor to enforce singleton pattern. Use OpenIt() to create and show the window.
    /// </summary>
    private AfterwardsActionWarningWindow(SessionAction action)
    {
        _window = this;
        Action = action;

        // Windows' own Mica/Acrylic backdrop only ever blurs the desktop wallpaper, never other
        // windows on screen (that's a platform limitation, not a bug here), so a real "see the blurred
        // desktop behind this" look is done by hand: grab a screenshot before this window exists to
        // cover it, then blur that bitmap as the window's own background.
        BackgroundSnapshot = CaptureVirtualScreen();

        InitializeComponent();

        // Spans the full virtual desktop (every monitor), not just the primary one, so the warning
        // cannot be missed regardless of which screen currently has the user's attention.
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        Opacity = 0;
        Loaded += (_, _) => AnimateIn();

        // IsCancel on the Cancel button should already route Escape to it, but that relies on
        // focus/command routing that a full-screen backdrop window doesn't always have - this
        // guarantees Escape always cancels regardless.
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(this, new RoutedEventArgs());
            }
        };

        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    // Corner preference and titlebar-background removal are handled internally by the ui:FluentWindow
    // base class via the WindowCornerPreference / ExtendsContentIntoTitleBar properties set in XAML.

    /// <summary>
    /// A one-shot screenshot of every monitor, taken right before this window is shown, used as the
    /// (blurred) window background. See the constructor for why this is done in software instead of
    /// via the OS's own window backdrop.
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

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int SrcCopy = 0x00CC0020;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private static BitmapSource CaptureVirtualScreen()
    {
        var left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        var screenDc = GetDC(IntPtr.Zero);
        var memDc = CreateCompatibleDC(screenDc);
        var bitmap = CreateCompatibleBitmap(screenDc, width, height);
        var oldBitmap = SelectObject(memDc, bitmap);

        BitBlt(memDc, 0, 0, width, height, screenDc, left, top, SrcCopy);

        SelectObject(memDc, oldBitmap);
        DeleteDC(memDc);
        ReleaseDC(IntPtr.Zero, screenDc);

        var snapshot = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        snapshot.Freeze();
        DeleteObject(bitmap);

        return snapshot;
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
