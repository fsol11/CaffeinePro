# Caffeine Pro
This application is designed to prevent your Windows computer from going into sleep mode. It's useful for situations where you need your computer to stay awake, but can't change the system-wide sleep settings, or don't want to remember to change them back later.

## How It Works

There are two methods to keep Windows active and each have their own pros and cons.

 - **Regular Method:** The application simulates a tiny amount of user input (pressing F15 - which is not available on the keyboard) every 59 seconds. This is enough to trick the system into thinking that the user is still active, so it doesn't go to sleep. Advantage of this method is that it perfectly matches the situation of a user working with the computer. It also prevents programs to detect inactivity; however, it also prevents screen saver to be activated. This is the code that does it:

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

 - **Screen Saver Allowed Method:** if this option is activated, program will use another method to keep Windows active. This is achieved by setting thread state:

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern uint SetThreadExecutionState(uint esFlags);
            private const uint EsContinuous = 0x80000000;
            private const uint EsSystemRequired = 0x00000001;
   
            SetThreadExecutionState(EsContinuous | EsSystemRequired);


## Installation

To install the application, follow these steps:
