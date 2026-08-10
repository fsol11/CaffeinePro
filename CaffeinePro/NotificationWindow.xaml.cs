using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using CaffeinePro.Classes;
using CaffeinePro.Services;
using Wpf.Ui.Controls;

namespace CaffeinePro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class NotificationWindow
{
    // Animation Parameters and variables
    private const double SlideDistance = 40;
    private static readonly Duration AnimDuration = new(TimeSpan.FromMilliseconds(450));
    private double _finalLeft;
    private double _finalTop;
    private double _slideDx;
    private double _slideDy;
    private bool _closingAnimated;


    // Singleton instance to ensure only one notification window is open at a time
    private static NotificationWindow? _window;

    /// <summary>
    /// Initializes the About window and sets up the commandline usage information text box
    /// </summary>
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
    /// Opens the notification window with the given awakeness information. If a notification is already open, it will be closed and replaced with the new one.
    /// </summary>
    /// <param name="aw"></param>
    public static void OpenIt(Awakeness aw)
    {
        App.CurrentApp.Dispatcher.Invoke(() =>
        {
            if (_window is { IsLoaded: true })
            {
                CloseIt();
            }

            _window = new NotificationWindow(aw);
            _window.Show();
        });
    }

    /// <summary>
    /// Private constructor to enforce singleton pattern. Use OpenIt() to create and show the window.
    /// </summary>
    /// <param name="aw"></param>
    private NotificationWindow(Awakeness aw)
    {
        _window = this;
        Awakeness = aw;
        InitializeComponent();
        Opacity = 0;
    }

    /// <summary>
    /// The awakeness the notification is offering to activate.
    /// </summary>
    public Awakeness Awakeness
    {
        get;
    }

    /// <summary>
    /// The action taken once the awakeness ends, or <see cref="SessionAction.None"/> when there is none.
    /// </summary>
    public SessionAction AfterwardsAction => App.CurrentApp.AppSettings.AfterwardsAction;

    private void NotificationWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            ApplyRoundedCorners(hwnd);
        }

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 0,
            CornerRadius = default,
            GlassFrameThickness = new Thickness(-1),
            ResizeBorderThickness = new Thickness(4),
            UseAeroCaptionButtons = false,
        });

        if (WindowBackdrop.IsSupported(WindowBackdropType.Mica) && WindowBackdrop.RemoveBackground(this))
        {
            WindowBackdrop.ApplyBackdrop(this, WindowBackdropType.Mica);
            WindowBackdrop.RemoveTitlebarBackground(this);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private static void ApplyRoundedCorners(IntPtr hwnd)
    {
        const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        const int DWMWCP_ROUND = 2;
        var preference = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
    }

    private void NotificationWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionNearTray();
        AnimateIn();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_closingAnimated)
        {
            e.Cancel = true;
            AnimateOut();
        }
        base.OnClosing(e);
    }

    private void AnimateIn()
    {
        Left = _finalLeft + _slideDx;
        Top = _finalTop + _slideDy;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(LeftProperty, new DoubleAnimation(_finalLeft, AnimDuration) { EasingFunction = ease });
        BeginAnimation(TopProperty, new DoubleAnimation(_finalTop, AnimDuration) { EasingFunction = ease });
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, AnimDuration) { EasingFunction = ease });
    }

    private void AnimateOut()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        var fade = new DoubleAnimation(0, AnimDuration) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            _closingAnimated = true;
            Close();
        };

        BeginAnimation(LeftProperty, new DoubleAnimation(_finalLeft + _slideDx, AnimDuration) { EasingFunction = ease });
        BeginAnimation(TopProperty, new DoubleAnimation(_finalTop + _slideDy, AnimDuration) { EasingFunction = ease });
        BeginAnimation(OpacityProperty, fade);
    }

    private void PositionNearTray()
    {
        const double margin = 8;

        var workArea = SystemParameters.WorkArea;
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        // Determine taskbar edge by comparing WorkArea to full screen bounds.
        // The system tray sits at the "far" end of the taskbar.
        // _slideDx/_slideDy: offset the window starts at (relative to final), animating toward 0.
        if (workArea.Bottom < screenHeight) // taskbar at bottom -> tray bottom-right, slide up
        {
            _finalLeft = workArea.Right - ActualWidth - margin;
            _finalTop = workArea.Bottom - ActualHeight - margin;
            _slideDx = 0;
            _slideDy = SlideDistance;
        }
        else if (workArea.Top > 0) // taskbar at top -> tray top-right, slide down
        {
            _finalLeft = workArea.Right - ActualWidth - margin;
            _finalTop = workArea.Top + margin;
            _slideDx = 0;
            _slideDy = -SlideDistance;
        }
        else if (workArea.Left > 0) // taskbar at left -> tray bottom-left, slide right
        {
            _finalLeft = workArea.Left + margin;
            _finalTop = workArea.Bottom - ActualHeight - margin;
            _slideDx = -SlideDistance;
            _slideDy = 0;
        }
        else if (workArea.Right < screenWidth) // taskbar at right -> tray bottom-right, slide left
        {
            _finalLeft = workArea.Right - ActualWidth - margin;
            _finalTop = workArea.Bottom - ActualHeight - margin;
            _slideDx = SlideDistance;
            _slideDy = 0;
        }
        else // fall back to bottom-right, slide up
        {
            _finalLeft = workArea.Right - ActualWidth - margin;
            _finalTop = workArea.Bottom - ActualHeight - margin;
            _slideDx = 0;
            _slideDy = SlideDistance;
        }
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.KeepAwakeService.ActivateDefault();
        CloseIt();
    }

    private void IgnoreForToday_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.KeepAwakeService.SetIgnoreUnlockNotificationToToday();
        CloseIt();
    }

    private void AskLater_Click(object sender, RoutedEventArgs e)
    {
        CloseIt();
    }

    private void AlwaysActivate_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.KeepAwakeService.ActivateDefault();
        App.CurrentApp.AppSettings.StartActive = true;
        CloseIt();
    }
}
