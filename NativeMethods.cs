using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LapseOTron
{
    internal static class NativeMethods
    {
        #region User32
        public const int CURSOR_SHOWING = 0x00000001;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, ref IntRect rect);


        //[DllImport("user32.dll")]
        //internal static extern IntPtr GetWindowRect(IntPtr hWnd, ref IntRect rect);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        internal static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        internal static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

        [DllImport("user32.dll")]
        public static extern bool DrawIcon(IntPtr hdc, int x, int y, IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        internal static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        #endregion

        #region Gdi32

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        internal static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, PatBltType dwRop);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);

        #endregion

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IntRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IconInfo
    {
        public bool fIcon;          // Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies 
        public int xHotspot;        // Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot 
        public int yHotspot;        // Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot 
        public IntPtr hbmMask;      // (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon, 
        public IntPtr hbmColor;     // (HBITMAP) Handle to the icon color bitmap. This member can be optional if this 
    }

    public struct Size
    {
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CursorInfo
    {
        public int cbSize;          // Specifies the size, in bytes, of the structure. 
        public int flags;           // Specifies the cursor state. This parameter can be one of the following values:
        public IntPtr hCursor;      // Handle to the cursor. 
        public POINT ptScreenPos;   // A POINT structure that receives the screen coordinates of the cursor. 
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// Blitter types
    /// </summary>
    public enum PatBltType
    {
        SrcCopy = 0x00CC0020,
        SrcPaint = 0x00EE0086,
        SrcAnd = 0x008800C6,
        SrcInvert = 0x00660046,
        SrcErase = 0x00440328,
        NotSrcCopy = 0x00330008,
        NotSrcErase = 0x001100A6,
        MergeCopy = 0x00C000CA,
        MergePaint = 0x00BB0226,
        PatCopy = 0x00F00021,
        PatPaint = 0x00FB0A09,
        PatInvert = 0x005A0049,
        DstInvert = 0x00550009,
        Blackness = 0x00000042,
        Whiteness = 0x00FF0062
    }
}
