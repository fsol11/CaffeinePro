## Caffeine Pro

Caffeine Pro prevents your Windows computer from going to sleep, locking, or showing you as "Away" in
communication apps. It's useful when you need the machine to stay awake but can't change the system-wide
power settings — or don't want to remember to change them back later.

It lives in the notification area (system tray), keeps itself out of the way, and stops on a schedule you choose.

![Screenshot](https://lotrasoft.com/assets/caffeine-pro-light-BlS7Td5k.webp "Light Screen Shot")
![Screenshot](https://lotrasoft.com/assets/caffeine-pro-dark-DYYnqE2s.webp "Dark Screen Shot")

## Features

- **Keeps Windows awake** using one of two methods (see [How It Works](#how-it-works)) — either by simulating
  real keyboard/mouse input, or by asking Windows directly while still allowing the screen saver.
- **Timed sessions:** stay awake indefinitely, for a relative duration ("for 2 hours"), or until an absolute
  time of day ("until 5:30 PM"). A time slider makes picking the end time quick.
- **Actions afterwards:** when the timer expires, Caffeine Pro can leave the session alone or perform
  **Lock**, **Sign Out**, **Quit** (the app), **Sleep**, **Hibernate**, **Shutdown**, **Force Shutdown**,
  **Restart**, or **Force Restart**.
- **Startup behavior:** choose a default awakeness and whether the app should *auto-activate*, *ask you*, or
  *stay inactive* at startup and after each unlock. The "ask me" prompt is a lightweight notification you can
  dismiss for the rest of the day.
- **Returns-to-desk detection:** the app also notices when the display wakes back up after being blanked for
  inactivity — not just session lock/unlock — so it can offer to reactivate when you actually come back.
- **Pause while on battery:** optionally suspend keeping-awake whenever the machine is unplugged. Power source
  changes are detected the moment the charger is pulled, not on the next tick.
- **Pause while locked:** keeping-awake is automatically suspended while the workstation is locked, and resumes
  on unlock.
- **Tray icon reflects state:** distinct icons for active, inactive, and temporarily paused, with the reason
  ("Paused - running on battery", "Paused - workstation locked") shown in the tooltip.
- **Auto start with Windows:** can register itself to launch at sign-in.
- **Light/dark theme:** built on the WPF-UI Fluent library; follows the Windows theme and switches live when
  you change it.
- **Single instance:** only one copy runs at a time. Launching a second one detects the first, forwards any
  command-line arguments to it over a named pipe, and exits.
- **Command line control:** activate, deactivate, set a duration, query status, or quit the running instance
  from a script or shortcut.

## System Requirements

- Windows 10 version 1809 (build 17763) or later, x86 / x64 / ARM64.
- .NET 10 Desktop Runtime (included in the installer).

## Installation

Grab an installer from the [releases page](https://github.com/fsol11/CaffeinePro/releases), or build from
source:

```
git clone https://github.com/fsol11/CaffeinePro.git
cd CaffeinePro
dotnet build CaffeinePro.sln -c Release
```

The repository also contains an MSIX packaging project (`CaffeinePro Setup`) and an Advanced Installer MSI
project (`CaffeinePro AdvInstaller MSI Setup`) used to produce the published packages.

## How It Works

There are two methods to keep Windows active, each with its own trade-off. You pick one under **Settings →
Method**.

### 1. Simulate keyboard and mouse usage (default)

Caffeine Pro sends real input events through `SendInput`, alternating randomly between pressing **F14**,
pressing **F15** (neither is present on a normal keyboard, so nothing in your applications reacts to them), and
nudging the mouse one pixel around a tiny square that returns the cursor to where it started.

Because these are genuine input events, they update `GetLastInputInfo` — which is what Microsoft Teams, Slack
and similar apps use to decide you're away. This method therefore keeps you shown as *available*. The trade-off
is that it also prevents the screen saver from ever starting.

Two details make it behave well in practice:

- **It only fires when you're actually idle.** A low-level keyboard/mouse hook tracks your real activity, and
  the signal is skipped entirely if you've touched the machine since the last tick.
- **The interval is randomized** between roughly 35 and 120 seconds on every tick, rather than being a fixed
  cadence.

```cs
public static void SendKeepAwakeSignal()
{
    switch (_random.Next(3))
    {
        case 0:
            PressF14();
            break;
        case 1:
            PressF15();
            break;
        default:
            MoveMouseSquare();
            break;
    }
}
```

### 2. Allow screen saver

In this mode no input is simulated. Instead the app tells Windows that the system is required, using
`SetThreadExecutionState`:

```cs
[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
private static extern uint SetThreadExecutionState(uint esFlags);

private const uint EsContinuous     = 0x80000000;
private const uint EsSystemRequired = 0x00000001;

SetThreadExecutionState(EsContinuous | EsSystemRequired);
```

The machine stays awake and the screen saver is still free to start, but `SetThreadExecutionState` does **not**
update `GetLastInputInfo` — so communication apps will still mark you as Away after a while.

Whenever the app is deactivated or paused, the execution state is released with `ES_CONTINUOUS` alone so
Windows resumes managing sleep normally.

## Command Line

```
Usage: CaffeinePro [Command] [options]

Commands:
    activate            activate (default)
    activeForX          activate for X minutes
    activeUntilX        activate until X (X = hh:mmtt)
    deactivate          deactivate the running instance
    exit | quit         exit the running instance

Options:
  -help                 show help
  -status               show the current status
  -allowss              allow screen saver (no keyboard/mouse simulation)
  -inactiveOnBattery    pause while running on battery
```

If an instance is already running, the arguments are forwarded to it through a named pipe and the new process
exits immediately — so `CaffeinePro.exe deactivate` from a script controls the copy already in your tray.

## Settings

Settings are stored per user as JSON at:

```
%AppData%\CaffeinePro\CaffeineProConfig.json
```

They are written immediately whenever a setting changes, so there is no explicit "save" step.

## Project Layout

| Path | Contents |
| --- | --- |
| `CaffeinePro/Services` | `KeepAwakeService` (core timer and state), `WindowsSessionService` (lock/unlock, session actions), `SystemActivityService` (display power and AC/battery notifications), `SingletonService` (mutex + named pipe) |
| `CaffeinePro/Classes` | `AppSettings`, `Awakeness` (the "until when" model), `KeyMouseSimulator`, `WindowsKeyboardMouseCapture`, command-line processing |
| `CaffeinePro/Controls` | Tray/settings UI: time slider, awakeness view, startup options, status |
| `CaffeinePro/Converters` | WPF value converters used by the XAML |
| `CaffeinePro Setup` | MSIX packaging project |
| `CaffeinePro AdvInstaller MSI Setup` | Advanced Installer MSI project |

## License

Caffeine Pro is licensed under the MIT License. See [LICENSE.txt](LICENSE.txt).

Copyright (c) 2026 Lotrasoft Inc.
