using Caffeine_Pro.Classes;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace Caffeine_Pro.WindowsAndControls;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class AboutWindow : Window
{
    public string Version
    {
        get => Assembly.GetExecutingAssembly().GetName().Version!.ToString();
    }
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
        const string str = """<html><head><meta http-equiv="Content-Type" content="text/html; charset=utf-8" /><meta http-equiv="Content-Style-Type" content="text/css" /><meta name="generator" content="Aspose.Words for Cloud for .NET 24.2.0" /><title></title></head><body style="line-height:108%; font-family:Segoe UI; font-size:11pt"><div><p style="margin-top:0pt; margin-bottom:8pt"><span style="font-weight:bold">Usage:</span><span> </span></p><p style="margin-top:0pt; margin-bottom:8pt; text-align:center; background-color:#d9d9d9"><span style="font-family:'Courier New'">CaffeinePro.exe [Command] [options]</span></p><p style="margin-top:0pt; margin-bottom:8pt"><span style="-aw-import:ignore">&#xa0;</span></p><p style="margin-top:0pt; margin-bottom:8pt; border:0.75pt solid #000000; padding:1pt 4pt; -aw-border:0.5pt single"><span style="font-weight:bold">NOTE:</span><span> If another instance of </span><span style="font-style:italic">Caffeine Pro</span><span> is already running all the command line commands and options are sent to that instance. </span></p><p style="margin-top:0pt; margin-bottom:8pt"><span style="-aw-import:ignore">&#xa0;</span></p><p style="margin-top:0pt; margin-bottom:8pt"><span style="font-weight:bold">Commands:</span></p><table cellspacing="0" cellpadding="0" style="border-collapse:collapse"><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>exit </span></p></td><td style="width:227.7pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>exits the instance</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>activate</span></p></td><td style="width:227.7pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>activate</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>deactivate</span></p></td><td style="width:227.7pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>deactivate</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>activeforX</span></p></td><td style="width:227.7pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>activate for X min</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>activeuntilX</span></p></td><td style="width:227.7pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>activate until X (X=hh:mmpp, e.g. 5:24PM)</span></p></td></tr></table><p style="margin-top:0pt; margin-bottom:8pt"><span style="-aw-import:ignore">&#xa0;</span></p><p style="margin-top:0pt; margin-bottom:8pt"><span style="font-weight:bold">Saved Options</span><span> (will be saved for future runs):</span></p><table cellspacing="0" cellpadding="0" style="border-collapse:collapse"><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span style="-aw-import:spaces">&#xa0; </span><span>-help</span></p></td><td style="width:204.95pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>show help</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span style="-aw-import:spaces">&#xa0; </span><span>-startinactive</span></p></td><td style="width:204.95pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>starts inactive</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span style="-aw-import:spaces">&#xa0; </span><span>-allowss</span></p></td><td style="width:204.95pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>allow screen saver. No mouse/key sim</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span style="-aw-import:spaces">&#xa0; </span><span>-cpuX</span></p></td><td style="width:204.95pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>deactivate when CPU below X%</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span style="-aw-import:spaces">&#xa0; </span><span>-deactivatewhenlocked</span></p></td><td style="width:204.95pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>deactivate when computer is locked</span></p></td></tr><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span style="-aw-import:spaces">&#xa0; </span><span>-deactivateonbattery</span></p></td><td style="width:204.95pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>deactivate when on battery</span></p></td></tr></table><p style="margin-top:0pt; margin-bottom:8pt"><span style="-aw-import:ignore">&#xa0;</span></p><p style="margin-top:0pt; margin-bottom:8pt"><span style="font-weight:bold">Unsaved Options</span><span>:</span></p><table cellspacing="0" cellpadding="0" style="border-collapse:collapse"><tr><td style="width:124.2pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span style="-aw-import:spaces">&#xa0; </span><span>-noicon</span></p></td><td style="width:204.95pt; padding-right:5.4pt; padding-left:5.4pt; vertical-align:top"><p style="margin-top:0pt; margin-bottom:0pt; font-size:11pt"><span>do not show icon in system tray</span></p></td></tr></table><p style="margin-top:0pt; margin-bottom:8pt"><span style="-aw-import:ignore">&#xa0;</span></p><p style="margin-top:0pt; margin-bottom:8pt"><span style="-aw-import:ignore">&#xa0;</span></p></div></body></html>""";
        webBrowser.NavigateToString(str);
    }

    private void CloseBtnClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        var uri = ((Hyperlink)sender).NavigateUri;
        Process.Start(new ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });
    }
}
