using System.Runtime.InteropServices;

namespace MazureTools.Core.Native;

/// <summary>Thin P/Invoke layer over built-in Windows APIs (used instead of third-party packages).</summary>
internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

    /// <summary>FILETIME values as 100ns tick counts.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

    /// <summary>Time since the last keyboard or mouse input anywhere in the session.</summary>
    internal static TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        var idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(idleMs);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    /// <summary>Keeps a floating window out of Alt+Tab and stops clicks on it from stealing keyboard focus.</summary>
    internal static void MakeNonActivatingToolWindow(IntPtr hwnd)
    {
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35; // Windows 11 only
    private const int DWMWA_TEXT_COLOR = 36;    // Windows 11 only

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    /// <summary>
    /// Colours the native title bar to match the app theme. Each attribute is best-effort: unsupported
    /// builds (e.g. the colour attributes on Windows 10) simply ignore it and keep the default title bar.
    /// </summary>
    /// <param name="captionRgb">Background as 0xRRGGBB.</param>
    /// <param name="textRgb">Text as 0xRRGGBB.</param>
    internal static void SetTitleBar(IntPtr hwnd, bool dark, int captionRgb, int textRgb)
    {
        var darkMode = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        var caption = ToColorRef(captionRgb);
        var text = ToColorRef(textRgb);
        _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
        _ = DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));

        // Make Windows repaint the non-client area now instead of at the next activation.
        _ = SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }

    // COLORREF is 0x00BBGGRR.
    private static int ToColorRef(int rgb) => ((rgb & 0xFF) << 16) | (rgb & 0xFF00) | ((rgb >> 16) & 0xFF);
}
