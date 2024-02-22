using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Timer = System.Timers.Timer;

namespace Caffeine_Pro;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private readonly Timer _timer = new (5000) // 5 seconds
    {
        AutoReset = false,
        Enabled = false,
        Site = null,
        SynchronizingObject = null
    };
    
    private readonly TaskbarIcon _tbIcon = new()
    {
        AllowDrop = false,
        CacheMode = null,
        Clip = null,
        ClipToBounds = false,
        Effect = null,
        Focusable = false,
        IsEnabled = false,
        IsHitTestVisible = false,
        IsManipulationEnabled = false,
        Opacity = 0,
        OpacityMask = null,
        RenderSize = default,
        RenderTransform = null,
        RenderTransformOrigin = default,
        SnapsToDevicePixels = false,
        Uid = null,
        Visibility = Visibility.Visible,
        BindingGroup = null,
        ContextMenu = null,
        Cursor = null,
        DataContext = null,
        FlowDirection = FlowDirection.LeftToRight,
        FocusVisualStyle = null,
        ForceCursor = false,
        Height = 0,
        HorizontalAlignment = HorizontalAlignment.Left,
        InputScope = null,
        Language = null,
        LayoutTransform = null,
        Margin = default,
        MaxHeight = 0,
        MaxWidth = 0,
        MinHeight = 0,
        MinWidth = 0,
        Name = null,
        OverridesDefaultStyle = false,
        Style = null,
        Tag = null,
        ToolTip = null,
        UseLayoutRounding = false,
        VerticalAlignment = VerticalAlignment.Top,
        Width = 0,
        Icon = null,
        CustomPopupPosition = null,
        IconSource = null,
        ToolTipText = null,
        TrayToolTip = null,
        TrayPopup = null,
        MenuActivation = PopupActivationMode.LeftOrRightClick,
        PopupActivation = PopupActivationMode.DoubleClick,
        DoubleClickCommand = null,
        DoubleClickCommandParameter = null,
        DoubleClickCommandTarget = null,
        LeftClickCommand = null,
        LeftClickCommandParameter = null,
        LeftClickCommandTarget = null,
        NoLeftClickDelay = false
    };

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const uint MOUSEEVENTF_MOVE = 0x0001;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load the icon from resources
        using var iconStream = Routines.GetResourceStream("Caffeine_Pro.Coffee.ico");
        _tbIcon.Icon = new(iconStream);

        _tbIcon.ToolTipText = "Caffeine Pro";

        // Create the context menu
        var menu = new ContextMenu();
        var exitItem = new MenuItem { Header = "Exit"};
        exitItem.Click += OnMenuExit;
        menu.Items.Add(exitItem);

        _tbIcon.ContextMenu = menu;

        StartMouseMovement();
    }

    private void StartMouseMovement()
    {
        _timer.Elapsed += (sender, args) =>
        {
            // Simulate mouse movement
            mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, IntPtr.Zero); // Move back to the original position
        };
        _timer.Start();
    }

    private void OnMenuExit(object sender, RoutedEventArgs e)
    {
        _tbIcon.Dispose(); // Removes the icon from the system tray
        Current.Shutdown(); // Closes the application
    }
    
}