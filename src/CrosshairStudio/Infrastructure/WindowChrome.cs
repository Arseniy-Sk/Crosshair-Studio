using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace CrosshairStudio.Infrastructure;

internal static class WindowChrome
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmWcpDontRound = 1;
    private const int DwmWcpRound = 2;

    public const double CornerRadius = 18;

    public static void Apply(Window window)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero)
            return;

        try
        {
            var maximized = window.WindowState == WindowState.Maximized;
            var preference = maximized ? DwmWcpDontRound : DwmWcpRound;
            DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));

            if (maximized)
            {
                SetWindowRgn(hwnd, IntPtr.Zero, true);
                return;
            }

            var scaling = Math.Max(0.5, window.RenderScaling);
            var bounds = window.Bounds;
            var width = Math.Max(1, (int)Math.Round(bounds.Width * scaling));
            var height = Math.Max(1, (int)Math.Round(bounds.Height * scaling));
            if (width < 8 || height < 8)
                return;

            var radius = Math.Max(8, (int)Math.Round(CornerRadius * scaling));
            var region = CreateRoundRectRgn(0, 0, width + 1, height + 1, radius * 2, radius * 2);
            if (region != IntPtr.Zero)
                SetWindowRgn(hwnd, region, true);
        }
        catch
        {
            // Square window is still usable.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);
}
