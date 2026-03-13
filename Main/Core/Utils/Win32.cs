using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Main.Core.Utils
{
    public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);
    public static class Win32
    {
        public const string
            User32 = "user32.dll",
            Gdi32 = "gdi32.dll",
            GdiPlus = "gdiplus.dll",
            Kernel32 = "kernel32.dll",
            Shell32 = "shell32.dll",
            MsImg = "msimg32.dll",
            NTdll = "ntdll.dll",
            DwmApi = "dwmapi.dll";

        [DllImport(Kernel32)]
        public static extern IntPtr LoadLibrary(string lpFileName);
        /// <summary>
        /// Synthesizes a keystroke. The system can use such a synthesized keystroke to generate a WM_KEYUP or WM_KEYDOWN message. The keyboard driver's interrupt handler calls the keybd_event function.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-keybd_event
        /// </summary>
        /// <param name="bvk"></param>
        /// <param name="bScan"></param>
        /// <param name="dwFlags"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport(User32)]
        public static extern void keybd_event(byte bvk, byte bScan, uint dwFlags, int dwExtraInfo);

        /// <summary>
        /// Copies the status of the 256 virtual keys to the specified buffer.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeyboardstate
        /// </summary>
        /// <param name="keyState"></param>
        /// <returns></returns>
        [DllImport(User32, EntryPoint = "GetKeyboardState", SetLastError = true)]
        public static extern int GetKeyboardState(byte[] keyState);

        /// <summary>
        /// Retrieves the position of the mouse cursor, in screen coordinates.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorpos
        /// </summary>
        /// <param name="lpPoint"></param>
        /// <returns></returns>
        [DllImport(User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Point lpPoint);

        /// <summary>
        /// Brings the thread that created the specified window into the foreground and activates the window. Keyboard input is directed to the window,
        /// and various visual cues are changed for the user. The system assigns a slightly higher priority to the thread that created the foreground window than it does to other threads.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow
        /// </summary>
        /// <param name="wndHandle">窗口句柄</param>
        /// <returns></returns>
        [DllImport(User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr wndHandle);

        /// <summary>
        /// Retrieves the thread identifier of the calling thread.
        /// reference: https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentthreadid
        /// </summary>
        /// <returns></returns>
        [DllImport(Kernel32)]
        public static extern int GetCurrentThreadId();

        /// <summary>
        /// Removes a hook procedure installed in a hook chain by the <see cref="SetWindowsHookEx"/> function.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unhookwindowshookex
        /// </summary>
        /// <param name="hookId">线程钩子Id</param>
        /// <returns></returns>
        [DllImport(User32)]
        public static extern bool UnhookWindowsHookEx(int hookId);

        /// <summary>
        /// Retrieves a handle to the window that contains the specified point.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-windowfrompoint
        /// </summary>
        /// <param name="Point"></param>
        /// <returns></returns>
        [DllImport(User32)]
        public static extern IntPtr WindowFromPoint(Point Point);

        /// <summary>
        /// Retrieves the name of the class to which the specified window belongs.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclassname
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpString"></param>
        /// <param name="nMaxCount"></param>
        /// <returns></returns>
        [DllImport(User32)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Passes the hook information to the next hook procedure in the current hook chain. A hook procedure can call this function either before or after processing the hook information.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-callnexthookex
        /// </summary>
        /// <param name="hookId">线程钩子Id</param>
        /// <param name="code"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport(User32)]
        public static extern int CallNextHookEx(int hookId, int code, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Determines the number of items in the specified menu.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmenuitemcount
        /// </summary>
        /// <param name="hMenu"></param>
        /// <returns></returns>
        [DllImport(User32)]
        public static extern int GetMenuItemCount(IntPtr hMenu);

        /// <summary>
        /// Retrieves the menu item identifier of a menu item located at the specified position in a menu.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmenuitemid
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="nPos"></param>
        /// <returns></returns>
        [DllImport(User32)]
        public static extern uint GetMenuItemID(IntPtr hMenu, int nPos);
        /// <summary>
        /// Changes an attribute of the specified window. The function also sets the 32-bit (long) value at the specified offset into the extra window memory.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowlonga
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="nIndex"></param>
        /// <param name="dwNewLong"></param>
        /// <returns></returns>
        [DllImport(User32)]
        public static extern long SetWindowLong(IntPtr hwnd, int nIndex, long dwNewLong);

        /// <summary>
        /// Appends a new item to the end of the specified menu bar, drop-down menu, submenu, or shortcut menu. You can use this function to specify the content, appearance, and behavior of the menu item.
        /// reference: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-appendmenua
        /// </summary>
        /// <param name="hMenu">A handle to the menu bar, drop-down menu, submenu, or shortcut menu to be changed.</param>
        /// <param name="uFlags"></param>
        /// <param name="uIDNewItem"></param>
        /// <param name="lpNewItem"></param>
        /// <returns></returns>
        [DllImport(User32, CharSet = CharSet.Auto)]
        public static extern bool AppendMenu(IntPtr hMenu, long uFlags, ulong uIDNewItem, string lpNewItem);

        /// <summary>
        /// Determines which menu item, if any, is at the specified location.
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-menuitemfrompoint
        /// </summary>
        /// <param name="hWnd">A handle to the window containing the menu. If this value is NULL and the hMenu parameter represents a popup menu, the function will find the menu window.</param>
        /// <param name="hMenu">A handle to the menu containing the menu items to hit test.</param>
        /// <param name="ptScreen">A structure that specifies the location to test. If hMenu specifies a menu bar, this parameter is in window coordinates. Otherwise, it is in client coordinates.</param>
        /// <returns>Returns the zero-based position of the menu item at the specified location or -1 if no menu item is at the specified location.</returns>
        [DllImport(User32)]
        public static extern int MenuItemFromPoint(IntPtr hWnd, IntPtr hMenu, Point ptScreen);

        /// <summary>
        ///  Copies the text string of the specified menu item into the specified buffer.
        ///  reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmenustringa
        /// </summary>
        /// <param name="hMenu">A handle to the menu</param>
        /// <param name="uIdItem">The menu item to be changed, as determined by the uFlag parameter</param>
        /// <param name="lpString">The buffer that receives the null-terminated string. If the string is as long or longer than lpString, 
        /// the string is truncated and the terminating null character is added. If lpString is NULL, the function returns the length of the menu string</param>
        /// <param name="nMaxCount">The maximum length, in characters, of the string to be copied. If the string is longer than the maximum specified in the nMaxCount parameter, 
        /// the extra characters are truncated. If nMaxCount is 0, the function returns the length of the menu string</param>
        /// <param name="uFlag">Indicates how the uIDItem parameter is interpreted. This parameter must be one of the following values 
        /// 0x00000000L Indicates that uIDItem gives the identifier of the menu item. If neither the MF_BYCOMMAND nor MF_BYPOSITION flag is specified, the MF_BYCOMMAND flag is the default flag.
        /// 0x00000400L Indicates that uIDItem gives the zero-based relative position of the menu item.</param>
        /// <returns>If the function succeeds, the return value specifies the number of characters copied to the buffer, not including the terminating null character.
        /// If the function fails, the return value is zero. If the specified item is not of type MIIM_STRING or MFT_STRING, then the return value is zero</returns>
        [DllImport(User32)]
        public static extern int GetMenuString(IntPtr hMenu, uint uIdItem, [Out] StringBuilder lpString, int nMaxCount, uint uFlag);

        /// <summary>
        /// Passes message information to the specified window procedure
        /// reference:https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-callwindowproca
        /// </summary>
        /// <param name="lpPrevWndFunc">The previous window procedure. If this value is obtained by calling the GetWindowLong function with the nIndex parameter set to GWL_WNDPROC or DWL_DLGPROC, 
        /// it is actually either the address of a window or dialog box procedure, or a special internal value meaningful only to CallWindowProc.</param>
        /// <param name="hWnd">A handle to the window procedure to receive the message</param>
        /// <param name="Msg">The message.</param>
        /// <param name="wParam">Additional message-specific information. The contents of this parameter depend on the value of the Msg parameter</param>
        /// <param name="lParam">Additional message-specific information. The contents of this parameter depend on the value of the Msg parameter</param>
        /// <returns>The return value specifies the result of the message processing and depends on the message sent</returns>
        [DllImport(User32)]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, long Msg, IntPtr wParam, IntPtr lParam);

        [DllImport(User32, EntryPoint = "IsZoomed")]
        public static extern bool IsZoomed(IntPtr windHanle);

        [DllImport(User32)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(User32, SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport(User32, CharSet = CharSet.Auto)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport(Gdi32)]
        public static extern bool MoveToEx(IntPtr hDC, IntPtr x, IntPtr y, ref Point lpPoint);

        [DllImport(Gdi32)]
        public static extern IntPtr LineTo(IntPtr hdc, IntPtr x, IntPtr y);

        [DllImport(Gdi32)]
        public static extern bool AngleArc(IntPtr hdc, int x, int y, int nRadius, float fStartAngle, float fSweepAngle);

        [DllImport(Gdi32)]
        public static extern bool ArcTo(IntPtr hdc, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

        [DllImport(Gdi32)]
        public static extern IntPtr SetROP2(IntPtr hdc, IntPtr fnDrawMode);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport(User32)]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport(User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport(User32, SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport(User32, SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

        [DllImport(Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport(User32)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport(User32, CharSet = CharSet.Unicode)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        /// <summary>
        /// 删除指定指针的对象
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [DllImport(Gdi32, EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObjec);
    }
}
