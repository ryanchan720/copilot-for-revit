using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Main.Core.Utils
{
    public static class Win32Helper
    {
        public static ImageSource CreateBitmapSource(this Bitmap bitmap)
        {
            var handle = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                Win32.DeleteObject(handle);
            }
        }

        /// <summary>
        /// 发送Esc键
        /// </summary>
        public static void SendEscKey()
        {
            Win32.keybd_event(27, 0, 0u, 0);
            Win32.keybd_event(27, 0, 2u, 0);
        }
        /// <summary>
        /// 检查大小写键是否为锁定
        /// </summary>
        /// <returns></returns>
        public static bool IsCapsLocked()
        {
            var array = new byte[256];
            Win32.GetKeyboardState(array);
            return array[0x14] == 1;
        }
        /// <summary>
        /// 是否在Revit的绘图区
        /// </summary>
        /// <returns></returns>
        public static bool IsInGraphicsArea()
        {
            if (Win32.GetCursorPos(out var point))
            {
                var window = Win32.WindowFromPoint(point);
                var className = new StringBuilder(32);
                Win32.GetClassName(window, className, className.MaxCapacity);
                var isInner = className.Length > 0 && Regex.IsMatch(className.ToString(), "AfxFrameOrView[0-9]+u", RegexOptions.IgnoreCase);
                className.Clear();
                return isInner;
            }
            return false;
        }
        static IntPtr GetGraphicsViewPtr(IntPtr rvtPtr)
        {
            IntPtr viewPtr = IntPtr.Zero;
            bool EnumCallBack(IntPtr hPtr, IntPtr para)
            {
                var className = new StringBuilder(32);
                var result = Win32.GetClassName(hPtr, className, className.Capacity);
                if (result != 0)
                {
                    if (className.ToString() == "AfxFrameOrView140u")
                    {
                        viewPtr = hPtr;
                        return false;
                    }
                }
                return true;
            }
            Win32.EnumChildWindows(rvtPtr, new EnumWindowProc(EnumCallBack), IntPtr.Zero);

            return viewPtr;
        }
    }
    /// <summary>
    /// reference:https://stackoverflow.com/questions/12877876/cast-intptr-to-cwpstruct
    /// for WH_CALLWNDPROC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CWPSTRUCT
    {
        public long lParam;
        public ulong wParam;
        public uint message;
        public IntPtr hwnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class KBDLLHOOKSTRUCT
    {
        public uint vkCode; //A virtual-key code. The code must be a value in the range 1 to 254.
        public uint scanCode; //A hardware scan code for the key.
        /*
         The extended-key flag, event-injected flags, context code, and transition-state flag. This member is specified as follows. An application can use the following values to test the keystroke flags. Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected. If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a process running at lower integrity level.
         */
        public KBDLLHOOKSTRUCTFlags flags;
        public uint time; //The time stamp for this message, equivalent to what GetMessageTime would return for this message.
        public UIntPtr dwExtraInfo; //Additional information associated with the message.
    }

    [Flags]
    public enum KBDLLHOOKSTRUCTFlags : uint
    {
        LLKHF_EXTENDED = 0x01,//Test the extended-key flag.
        LLKHF_INJECTED = 0x10,//Test the event-injected (from any process) flag.
        LLKHF_ALTDOWN = 0x20,//	Test the context code.
        LLKHF_UP = 0x80,//Test the transition-state flag.
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

