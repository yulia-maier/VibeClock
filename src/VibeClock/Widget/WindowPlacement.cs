using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using VibeClock.Settings;

namespace VibeClock.Widget;

internal static class WindowPlacement
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern nint MonitorFromRect(ref Rect rect, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);

    public static void Restore(Window window, WidgetSettings settings)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (!GetWindowRect(hwnd, out var bounds)) return;
        int width = bounds.Right - bounds.Left, height = bounds.Bottom - bounds.Top;
        bool saved = settings.Left.HasValue && settings.Top.HasValue;
        int x = saved ? (int)Math.Clamp(settings.Left!.Value, -100000, 100000) : bounds.Left;
        int y = saved ? (int)Math.Clamp(settings.Top!.Value, -100000, 100000) : bounds.Top;
        var desired = new Rect { Left = x, Top = y, Right = x + width, Bottom = y + height };
        var monitor = MonitorFromRect(ref desired, 2); // Nearest monitor, including after unplugging a screen.
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;
        int maxX = Math.Max(info.Work.Left, info.Work.Right - width);
        int maxY = Math.Max(info.Work.Top, info.Work.Bottom - height);
        x = saved ? Math.Clamp(x, info.Work.Left, maxX) : Math.Max(info.Work.Left, maxX - 28);
        y = saved ? Math.Clamp(y, info.Work.Top, maxY) : Math.Max(info.Work.Top, maxY - 28);
        SetWindowPos(hwnd, 0, x, y, 0, 0, 0x0015); // Keep size, z-order and activation.
    }

    public static void Capture(Window window, WidgetSettings settings)
    {
        if (!GetWindowRect(new WindowInteropHelper(window).Handle, out var bounds)) return;
        // Physical desktop pixels avoid ambiguous coordinates on mixed DPI monitors.
        settings.Left = bounds.Left;
        settings.Top = bounds.Top;
    }
}
