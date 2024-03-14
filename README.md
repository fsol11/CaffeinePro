## Caffeine Pro

This application is designed to prevent your Windows computer from going into sleep mode. It's useful for situations where you need your computer to stay awake, but can't change the system-wide sleep settings, or don't want to remember to change them back later.

## Features

- **Keeps Windows Awake:** by simulating a key press every 59 seconds. This is enough to trick the system into thinking that the user is still active, so it doesn't go to sleep. This method also prevents programs to detect user inactivity.
- **Screen Saver Allowed:** In this mode, instead of a keypress simulation, a special thread state is used to keep Windows awake, but this method does not prevent programs to detect user inactivity.
- **Singleton Instance:** Only one instance of the application is allowed in memory. When starting a new instance, it detects the already running application and automatically exits; however, any commandline option that is passed to the second instance, is sent to the first instance through a names pipeline.
- **Support Dark/Light themes:** It uses Microsoft Fluent UI and supports both light and dark themes.
![Screenshot](https://lotrasoft.com/wp-content/uploads/2024/03/Screenshot-2024-03-15-084955.png "Screen Shot")
![Screenshot](https://lotrasoft.com/wp-content/uploads/2024/03/Screenshot-2024-03-15-085027.png "Screen Shot")
- **Auto Start With Windows:** The program can be set to start automatically with Window.
- **Actions after Deactivation:** After the set time, program can make Windows session: 
    **Exit**, **Lock**, **Sign Out**, **Hibernate**, **Sleep**, **Shutdown**, **Force Shutdown**, **Restart**, **Force Restart**. 
- **Deactivate on Battery:** The program can be set to deactivate when the computer is running on battery.
- **Deactivate on CPU Idle:** The program can be set to deactivate when the CPU is below a certain percentage.
- **Stay Active When Locked:** The program by default deactivates when locked.
**NOTE:** When program is started, it becomes active (indefinitely) by default. If you want to start it inactive, you can use the commandline option `-startinactive`.

## System Requirements

- Windows 7 or later.
- .Net Core 8.0 or later. (It's included in the installer)

## How It Works

There are two methods to keep Windows active and each have their own pros and cons.

**Key Press Simulation:** The application simulates pressing F15 (which is not typically available on the keyboard) every 59 seconds. This is enough to trick the system into thinking that the user is still active, so it doesn't go to sleep. Advantage of this method is that it perfectly matches the situation of a user working with the computer which means it successfully prevents programs to detect inactivity; however, it also prevents screen saver to be activated. This is the code that does it:

```cs
  [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
     private static extern uint SetThreadExecutionState(uint esFlags);
     private const uint EsContinuous = 0x80000000;
     private const uint EsSystemRequired = 0x00000001;

     SetThreadExecutionState(EsContinuous | EsSystemRequired);
```

**Screen Saver Allowed Method:** if this option is activated, program will use another method to keep Windows active. This is achieved by setting thread state:

```cs
public static void PressF15()
 {
     var inputs = new Input[2];

     // Press the F15 key
     inputs[0].type = INPUT_KEYBOARD;
     inputs[0].U.ki.wVk = (ushort)VK_F15;

     // Release the F15 key
     inputs[1].type = INPUT_KEYBOARD;
     inputs[1].U.ki.wVk = (ushort)VK_F15;
     inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;

     SendInput((uint)inputs.Length, inputs, Input.Size);
 }
```

## Singleton Instance

Only one instance of the application is allowed in memory. When starting a new instance, it detects the already running application and automatically exists; however, any option that is passed to the second instance, is sent to the first instance through a names pipeline.

This method allows controlling the program through commandline. For example to instruct the program to exit, this command can be executed:

```typescript
"Caffeine Pro.exe" exit
```

## Commandline Usage

```typescript
CaffeinePro.exe [Command] [options]
```

**NOTE:** If another instance of _Caffeine Pro_ is already running all the command line commands and options are sent to that instance.

**Commands:**

<table><tbody><tr><td style="width:200px">activate</td><td style="width:400px">activate</td></tr><tr><td>activeforX</td><td>activate for X min</td></tr><tr><td>activeuntilX</td><td>activate until X (X=hh:mmpp, e.g. 5:24PM)</td></tr><tr><td>deactivate</td><td>deactivate **</td></tr><tr><td>exit</td><td>exits the instance **</td></tr></tbody></table>

**Options:**

<table><tbody><tr><td style="width:200px">&nbsp; -help</td><td style="width:400px">Show help</td></tr><tr><td>&nbsp; -resetoptions</td><td>Reset all options to false **</td></tr><tr><td>&nbsp;</td><td>&nbsp;</td></tr><tr><td>&nbsp; -startinactive</td><td>starts inactive</td></tr><tr><td>&nbsp; -allowss</td><td>allow screen saver. No mouse/key sim</td></tr><tr><td>&nbsp; -cpuX</td><td>deactivate when CPU below X% *</td></tr><tr><td>&nbsp; -deactivewhenlocked</td><td>deactivate when computer is locked *</td></tr><tr><td>&nbsp; -deactivateonbattery</td><td>deactivate when on system is on battery *</td></tr><tr><td>&nbsp; -noicon</td><td>Do not show the icon in system tray</td></tr></tbody></table>

\*: When deactivated, it it will stay inactive until manually activated again.

\*\*: specifically designed for controlling existing instance in memory.

## Installation

To install the application, follow these steps: