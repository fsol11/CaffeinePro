using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using Wpf.Ui.Controls;

namespace CaffeinePro;

/// <summary>
/// Base class for the small borderless popups (unlock prompt, afterwards-action countdown) that
/// slide in near the system tray. Centralizes the rounded corners, Mica backdrop, tray-relative
/// positioning, slide/fade animation and Escape-to-close behavior shared by all of them.
/// </summary>
public abstract class NotificationWindowBase : Window
{
    private const double SlideDistance = 40;
    private static readonly Duration AnimDuration = new(TimeSpan.FromMilliseconds(450));
    private double _finalLeft;
    private double _finalTop;
    private double _slideDx;
    private double _slideDy;
    private bool _closingAnimated;

    protected NotificationWindowBase()
    {
        Opacity = 0;

        Loaded += (_, _) =>
        {
            PositionNearTray();
            AnimateIn();
        };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };
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
}
