using System.Windows;
using System.Windows.Interop;
using CaffeinePro.Classes;
using Windows.Services.Store;
using WinRT.Interop;

namespace CaffeinePro.Windows;

/// <summary>
/// Asks, once the app has been in use for a while, whether it is working well. A yes opens the
/// Microsoft Store's own rating dialog; a no offers to send feedback to Lotrasoft instead, while
/// still leaving the Store rating one click away.
/// </summary>
/// <remarks>
/// Follows the Store's guidance for in-app rating requests: asked only after real use, at a
/// moment the app has just done its job, at most once every <see cref="CooldownDays"/> days, and
/// never again once the user has rated or sent feedback. Store policy also forbids sending only
/// the satisfied users to the Store, which is why the "no" step keeps a way to rate as well.
/// </remarks>
public partial class RatingPromptWindow
{
    private const int MinUsageDays = 5;
    private const int CooldownDays = 90;

    private static RatingPromptWindow? _window;

    /// <summary>
    /// Set by the -ratingTest switch: the prompt is shown regardless of its schedule, the About
    /// window offers the rating link even outside the Store build, and the Store's answer is shown
    /// in a message box rather than acted on silently.
    /// </summary>
    public static bool TestMode
    {
        get;
        private set;
    }

    /// <summary>
    /// Starts a test run of the whole procedure: forgets any earlier answer and shows the prompt now.
    /// </summary>
    public static void StartTest()
    {
        TestMode = true;

        var settings = App.CurrentApp.AppSettings;
        settings.RatingPromptDone = false;
        settings.RatingPromptDate = default;

        Open();
    }

    /// <summary>
    /// Shows the prompt if it is due. Called after the user has switched the app on themselves.
    /// </summary>
    /// <remarks>
    /// Only in the Store build: the rating dialog needs the package identity, and an MSI install
    /// has no Store listing of its own to rate.
    /// </remarks>
    public static void OpenIfDue()
    {
        var settings = App.CurrentApp.AppSettings;

        if (!Routines.IsPackaged
            || settings.RatingPromptDone
            || settings.UsageDays < MinUsageDays
            || DateTime.Today < settings.RatingPromptDate.AddDays(CooldownDays))
        {
            return;
        }

        Open();
    }

    private static void Open()
    {
        if (_window != null)
        {
            return;
        }

        App.CurrentApp.AppSettings.RatingPromptDate = DateTime.Today;

        App.CurrentApp.Dispatcher.InvokeAsync(async () =>
        {
            // A moment's pause, so it does not land on top of the menu or prompt the activation came from.
            await Task.Delay(TimeSpan.FromSeconds(2));
            _window = new RatingPromptWindow();
            _window.Show();
        });
    }

    private RatingPromptWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _window = null;
    }

    private async void Yes_Click(object sender, RoutedEventArgs e)
    {
        // Hidden, not closed: the Store dialog needs an owner window, and this one is Topmost and
        // would otherwise cover it.
        Hide();
        await RequestStoreRatingAsync(this);
        Close();
    }

    /// <summary>
    /// Opens the Microsoft Store's own rating dialog over <paramref name="owner"/>. Once the user
    /// has submitted a rating, the prompt is not shown again.
    /// </summary>
    public static async Task RequestStoreRatingAsync(Window owner)
    {
        // Without a package identity the Store API does not throw - it brings the whole process
        // down with an access violation inside Windows.ApplicationModel.Store.dll.
        if (!Routines.IsPackaged)
        {
            ShowTestResult("Not called: the Store rating dialog needs the packaged app. "
                           + "Run scripts\\test-package.ps1 and test there.");
            return;
        }

        try
        {
            var context = StoreContext.GetDefault();
            InitializeWithWindow.Initialize(context, new WindowInteropHelper(owner).Handle);
            var result = await context.RequestRateAndReviewAppAsync();

            if (result.Status == StoreRateAndReviewStatus.Succeeded)
            {
                App.CurrentApp.AppSettings.RatingPromptDone = true;
            }

            ShowTestResult($"Status: {result.Status}{Environment.NewLine}"
                           + $"Updated existing rating: {result.WasUpdated}{Environment.NewLine}"
                           + $"Error: {result.ExtendedError?.Message}{Environment.NewLine}"
                           + $"Data: {result.ExtendedJsonData}");
        }
        catch (Exception ex)
        {
            // Rating is a courtesy, so a failure is never put in front of a customer.
            ShowTestResult(ex.ToString());
        }
    }

    private static void ShowTestResult(string text)
    {
        if (TestMode)
        {
            Dialogs.Show(text, "Store rating test");
        }
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        QuestionPanel.Visibility = Visibility.Hidden;
        FeedbackPanel.Visibility = Visibility.Visible;
    }

    private void SendFeedback_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.AppSettings.RatingPromptDone = true;
        Routines.OpenHyperlink(App.FeedbackUrl);
        ShowTestResult("Feedback page opened. The prompt will not be shown again.");
        Close();
    }
}
