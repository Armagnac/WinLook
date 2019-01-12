using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinLook
{
    public enum ShellProcMessage
    {
        HShellWindowActivated = 4,
        HShellRudeApplication = 0x8004,
        HShellFlash = 0x8006
    }

    public enum GetIconFlags
    {
        IconSmall = 0,
        IconBig = 1,
        IconSmall2 = 2
    }

    public enum GetClassLongIndices
    {
        HIcon = -14,
        HIconSmall = -34
    }

    public enum WindowMessages
    {
        GetIcon = 0x007F,
        SystemCommand = 0x112,
        QueryDragIcon = 0x0037
    }

    public enum SendMessageTimeoutFlags
    {
        AbortIfHung = 0x0002,
        Block = 0x0001,
        Normal = 0x0000,
        NoTimeoutIfNothing = 0x0008,
    }

    public enum SysCommandFlags
    {
        Restore = 0xF120,
        Close = 0xF060
    }

    public enum ShowWindowCommands
    {
        ForceMinimize = 11,
        Hide = 0,
        Maximize = 3,
        Minimize = 6,
        Restore = 9,
        Show = 5,
        ShowDefault = 10,
        ShowMaximized = 3,
        ShowMinimized = 2,
        ShowMinimizedNotActivated = 7,
        ShowNotActivated = 8,
        ShowNormalNotActivated = 4,
        ShowNormal = 1
    }

    [Flags]
    internal enum SHGetFileInfoFlags : int
    {
        /// <summary>
        /// Get icon.
        /// </summary>
        Icon = 0x000000100,

        /// <summary>
        /// Get display name.
        /// </summary>
        DisplayName = 0x000000200,

        /// <summary>
        /// Get type name.
        /// </summary>
        TypeName = 0x000000400,

        /// <summary>
        /// Get attributes.
        /// </summary>
        Attributes = 0x000000800,

        /// <summary>
        /// Get icon location.
        /// </summary>
        IconLocation = 0x000001000,

        /// <summary>
        /// Return exe type.
        /// </summary>
        ExeType = 0x000002000,

        /// <summary>
        /// Get system icon index.
        /// </summary>
        SysIconIndex = 0x000004000,

        /// <summary>
        /// Put a link overlay on icon.
        /// </summary>
        LinkOverlay = 0x000008000,

        /// <summary>
        /// Show icon in selected state.
        /// </summary>
        Selected = 0x000010000,

        /// <summary>
        /// Get only specified attributes.
        /// </summary>
        AttributeSpecified = 0x000020000,

        /// <summary>
        /// Get large icon.
        /// </summary>
        LargeIcon = 0x000000000,

        /// <summary>
        /// Get small icon.
        /// </summary>
        SmallIcon = 0x000000001,

        /// <summary>
        /// Get open icon.
        /// </summary>
        OpenIcon = 0x000000002,

        /// <summary>
        /// Get shell size icon.
        /// </summary>
        ShellIconSize = 0x000000004,

        /// <summary>
        /// Path is a PIDL.
        /// </summary>
        PIDL = 0x000000008,

        /// <summary>
        /// Use passed FileAttribute.
        /// </summary>
        UseFileAttributes = 0x000000010,

        /// <summary>
        /// Apply the appropriate overlays.
        /// </summary>
        AddOverlays = 0x000000020,

        /// <summary>
        /// Get the index of the overlay in the upper 8 bits of the icon.
        /// </summary>
        OverlayIndex = 0x000000040,
    }
}
