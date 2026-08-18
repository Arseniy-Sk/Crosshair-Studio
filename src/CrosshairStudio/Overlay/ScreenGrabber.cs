using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace CrosshairStudio.Overlay;

internal sealed class ScreenGrabber : IDisposable
{
    private WriteableBitmap? _a;
    private WriteableBitmap? _b;
    private bool _flip;
    private int _width;
    private int _height;

    public WriteableBitmap? Capture(int x, int y, int width, int height)
    {
        if (!OperatingSystem.IsWindows() || width < 8 || height < 8)
            return null;

        try
        {
            EnsureBitmaps(width, height);
            var target = _flip ? _b : _a;
            if (target == null)
                return null;

            var screen = GetDC(IntPtr.Zero);
            if (screen == IntPtr.Zero)
                return null;

            var memory = CreateCompatibleDC(screen);
            var handle = CreateCompatibleBitmap(screen, width, height);
            var old = SelectObject(memory, handle);
            BitBlt(memory, 0, 0, width, height, screen, x, y, SrcCopy);

            var info = new BitmapInfo
            {
                Size = 40,
                Width = width,
                Height = -height,
                Planes = 1,
                BitCount = 32,
                Compression = 0
            };

            using (var fb = target.Lock())
                GetDIBits(memory, handle, 0, (uint)height, fb.Address, ref info, 0);

            SelectObject(memory, old);
            DeleteObject(handle);
            DeleteDC(memory);
            ReleaseDC(IntPtr.Zero, screen);
            _flip = !_flip;
            return target;
        }
        catch
        {
            return null;
        }
    }

    public static (int x, int y) CursorPosition()
    {
        if (!OperatingSystem.IsWindows() || !GetCursorPos(out var point))
            return (0, 0);
        return (point.X, point.Y);
    }

    private void EnsureBitmaps(int width, int height)
    {
        if (_a != null && _b != null && _width == width && _height == height)
            return;
        _a?.Dispose();
        _b?.Dispose();
        _width = width;
        _height = height;
        _a = Create(width, height);
        _b = Create(width, height);
    }

    private static WriteableBitmap Create(int width, int height)
        => new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);

    public void Dispose()
    {
        _a?.Dispose();
        _b?.Dispose();
        _a = null;
        _b = null;
    }

    private const int SrcCopy = 0x00CC0020;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public int Size;
        public int Width;
        public int Height;
        public short Planes;
        public short BitCount;
        public int Compression;
        public int SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public int ClrUsed;
        public int ClrImportant;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines, IntPtr bits, ref BitmapInfo info, uint usage);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);
}
