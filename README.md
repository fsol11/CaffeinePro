## Caffeine Pro

This application is designed to prevent your Windows computer from going into sleep mode. It's useful for situations where you need your computer to stay awake, but can't change the system-wide sleep settings, or don't want to remember to change them back later.

![Screenshot](https://lotrasoft.com/wp-content/uploads/2024/07/CaffeinePro-2.1.523-Light.webp "Light Screen Shot")
![Screenshot](https://lotrasoft.com/wp-content/uploads/2024/07/CaffeinePro-2.1.523-Dark.webp "Dark Screen Shot")

## Features

- **Keeps Windows Awake:** by simulating a key press every at random intervals. This is enough to trick the system into thinking that the user is still active, so it doesn't go to sleep. This method also prevents programs to detect user inactivity.
- **Screen Saver Allowed:** In this mode, instead of a keypress simulation, a special thread state is used to keep Windows awake, but this method does not prevent programs like Microsoft Teams to detect user inactivity.
- **Singleton Instance:** Only one instance of the application is allowed in memory. When starting a new instance, it detects the already running application and automatically exits; however, any commandline option that is passed to the second instance, is sent to the first instance through a names pipeline.
- **Support Dark/Light themes:** It uses Microsoft Fluent UI and supports both light and dark themes.
- **Auto Start With Windows:** The program can be set to start automatically with Window.
- **Actions after Deactivation:** After the set time, program can make Windows session: 
    **Exit**, **Lock**, **Sign Out**, **Hibernate**, **Sleep**, **Shutdown**, **Force Shutdown**, **Restart**, **Force Restart**. 
- **Inactive on Battery:** The program can be set to temporarily become inactive when the computer is running on battery.
- **Inactive on CPU Idle:** The program can be set to temporarily become inactive when the CPU is below a certain percentage.
- **Inactive When Locked:** The program can be set to temporarily become inactive when locked.


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
