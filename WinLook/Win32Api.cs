using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinLook
{
    public class Win32Api
    {
        public static Int32 GetWindowProcessId(IntPtr windowHandle)
        {
            var processId = 0;
            GetWindowThreadProcessId(windowHandle, ref processId);
            return processId;
        }

        public static String GetWindowTitle(IntPtr windowHandle)
        {
            var stringBuilder = new StringBuilder(260);

            if (GetWindowText(windowHandle, stringBuilder, stringBuilder.Capacity) == 0)
                return String.Empty;

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Get the associated Icon for a file or application.
        /// This method always returns an icon -i f the strPath is invalid or there is no icon, the default icon is returned
        /// </summary>
        /// <param name="filePath">full path to the file</param>
        /// <param name="small">if true, the 16x16 icon is returned otherwise the 32x32</param>
        public static ImageSource GetIconFromFile(String filePath, Boolean small)
        {
            var fileInfo = new SHFileInfo();

            var fileInfoSize = Marshal.SizeOf(fileInfo);
            var flags = SHGetFileInfoFlags.Icon | SHGetFileInfoFlags.UseFileAttributes;
            if (small)
                flags |= SHGetFileInfoFlags.SmallIcon;
            else
                flags |= SHGetFileInfoFlags.LargeIcon;

            SHGetFileInfo(filePath, 256, out fileInfo, (UInt32)fileInfoSize, flags);
            return GetIcon(fileInfo.IconHandle);
        }

        public static ImageSource GetIcon(IntPtr iconHandle)
        {
            if (iconHandle != IntPtr.Zero)
            {
                try
                {
                    return Imaging.CreateBitmapSourceFromHIcon(iconHandle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        public static IntPtr FindWindow(String windowName)
        {
            return FindWindow(null, windowName);
        }

        public static IntPtr FindControl(String windowName, String className)
        {
            var windowHandle = FindWindow(windowName);
            if (windowHandle == IntPtr.Zero)
                return IntPtr.Zero;

            var result = IntPtr.Zero;
            EnumWindowDelegate findControlInChildren = null;
            findControlInChildren = (childWindowHandle, lParam) =>
            {
                result = FindWindowEx(childWindowHandle, IntPtr.Zero, className, null);
                if (result != IntPtr.Zero)
                    return false;

                EnumChildWindows(childWindowHandle, findControlInChildren, IntPtr.Zero);
                return (result == IntPtr.Zero);
            };
            
            EnumChildWindows(windowHandle, findControlInChildren, IntPtr.Zero);

            return result;
        }

        public static Boolean ShowWindow(IntPtr windowHandle)
        {
            if (SetForegroundWindow(windowHandle))
            {
                if (IsIconic(windowHandle))
                    SendMessage(windowHandle, WindowMessages.SystemCommand, (Int32)SysCommandFlags.Restore, 0);

                return true;
            }

            return false;
        }

        // ReSharper disable once InconsistentNaming
        public struct SHFileInfo
        {
            public IntPtr IconHandle;
            public Int32 IconIndex;
            public UInt32 Attributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String DisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public String TypeName;
        }

        public delegate Boolean EnumWindowDelegate(IntPtr windowHandle, Boolean lParam);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern Int32 StrCmpLogicalW(String x, String y);

        [DllImport("User32.dll")]
        public static extern Int32 GetWindowThreadProcessId(IntPtr windowHandle, ref Int32 processId);

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern Int32 GetWindowText(IntPtr windowHandle, StringBuilder title, Int32 windowTextLength);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.SysUInt)]
        public static extern IntPtr SendMessage(IntPtr windowHandle, WindowMessages message, Int32 wParam, Int32 lParam);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.SysUInt)]
        public static extern IntPtr SendMessageTimeout(IntPtr windowHandle, WindowMessages message, Int32 wParam, Int32 lParam, SendMessageTimeoutFlags flags, Int32 timeout, out IntPtr result);

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(String className, String windowName);

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr windowHandle, IntPtr windowHandleChildAfter, String className, String windowName);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean EnumDesktopWindows(IntPtr desktopHandle, EnumWindowDelegate callback, IntPtr lParam);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean EnumThreadWindows(Int32 threadId, EnumWindowDelegate callback, IntPtr lParam);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean EnumChildWindows(IntPtr parentWindowHandle, EnumWindowDelegate callback, IntPtr lParam);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean IsIconic(IntPtr windowHandle);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean IsWindowVisible(IntPtr windowHandle);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean SetForegroundWindow(IntPtr windowHandle);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean RegisterShellHookWindow(IntPtr windowHandle);

        [DllImport("User32.dll")]
        public static extern IntPtr GetClassLong(IntPtr windowHandle, GetClassLongIndices index);

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SHGetFileInfo(String path, Int32 fileAttributes, out SHFileInfo fileInfo, UInt32 fileInfoSize, SHGetFileInfoFlags flags);
    }
}
