using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CaffeinePro.Classes
{
    internal class WindowsKeyboardMouseCapture
    {
        // ReSharper disable once InconsistentNaming
        private const int WH_KEYBOARD_LL = 13;
        // ReSharper disable once InconsistentNaming
        private const int WH_MOUSE_LL = 14;
        private IntPtr _keyboardHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;
        private LowLevelKeyboardProc? _keyboardProc;
        private LowLevelMouseProc? _mouseProc;
        ~WindowsKeyboardMouseCapture()
        {
            Unhook();
        }

        private static IntPtr SetHook(Delegate? proc, int hookType)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            Debug.Assert(curModule != null, nameof(curModule) + " != null");
            return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        public bool Hook()
        {
            if (Hooked)
            {
                return true;
            }

            Hooked = true;

            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            _keyboardHookId = SetHook(_keyboardProc, WH_KEYBOARD_LL);
            _mouseHookId = SetHook(_mouseProc, WH_MOUSE_LL);

            return true;
        }

        public void Unhook()
        {
            if (!Hooked)
            {
                return;
            }

            UnhookWindowsHookEx(_keyboardHookId);
            UnhookWindowsHookEx(_mouseHookId);

            Hooked = false;
        }

        public bool Hooked
        {
            get;
            private set;
        }

        public DateTime LastActivity
        {
            get;
            private set;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            LastActivity = DateTime.Now;
            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            LastActivity = DateTime.Now;
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate? fn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}
