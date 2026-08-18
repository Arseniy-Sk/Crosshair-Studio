using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace CrosshairStudio.Overlay;

internal static class NativeOverlay
{
    private static readonly HashSet<IntPtr> Stack = [];

    public static void Apply(Window window, bool clickThrough, bool alwaysOnTop, bool noActivate = true)
    {
        window.Topmost = alwaysOnTop;
        window.IsHitTestVisible = !clickThrough;

        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
            return;

        try
        {
            if (OperatingSystem.IsWindows())
                ApplyWindows(handle, clickThrough, alwaysOnTop, noActivate);
            else if (OperatingSystem.IsMacOS())
                ApplyMac(handle, clickThrough, alwaysOnTop);
            else if (OperatingSystem.IsLinux())
                ApplyLinux(handle, clickThrough);
        }
        catch
        {
            // Overlay still shows; click-through may be unavailable on this compositor.
        }
    }

    public static void Forget(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero)
            Stack.Remove(handle);
    }

    public static void RaiseAll()
    {
        if (!OperatingSystem.IsWindows())
            return;
        foreach (var hwnd in Stack.ToArray())
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            {
                Stack.Remove(hwnd);
                continue;
            }

            Raise(hwnd);
        }
    }

    private static void ApplyWindows(IntPtr hwnd, bool clickThrough, bool alwaysOnTop, bool noActivate)
    {
        var style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        style |= WsExToolWindow | WsExLayered;
        if (alwaysOnTop)
            style |= WsExTopmost;
        else
            style &= ~WsExTopmost;
        if (noActivate)
            style |= WsExNoActivate;
        else
            style &= ~WsExNoActivate;
        if (clickThrough)
            style |= WsExTransparent;
        else
            style &= ~WsExTransparent;
        SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(style));

        if (alwaysOnTop)
        {
            Stack.Add(hwnd);
            Raise(hwnd);
        }
        else
            Stack.Remove(hwnd);
    }

    private static void Raise(IntPtr hwnd)
        => SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);

    private static void ApplyMac(IntPtr nsView, bool clickThrough, bool alwaysOnTop)
    {
        var windowSel = sel_registerName("window");
        var nsWindow = IntPtr_objc_msgSend(nsView, windowSel);
        if (nsWindow == IntPtr.Zero)
            return;

        void_objc_msgSend_bool(nsWindow, sel_registerName("setIgnoresMouseEvents:"), clickThrough);
        void_objc_msgSend_bool(nsWindow, sel_registerName("setHidesOnDeactivate:"), false);
        if (alwaysOnTop)
            void_objc_msgSend_long(nsWindow, sel_registerName("setLevel:"), 25);
    }

    private static void ApplyLinux(IntPtr xid, bool clickThrough)
    {
        if (!clickThrough)
            return;

        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            return;

        try
        {
            XShapeCombineMask(display, xid, ShapeInput, 0, 0, IntPtr.Zero, ShapeSet);
            XFlush(display);
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExTopmost = 0x00000008;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const int ShapeInput = 2;
    private const int ShapeSet = 0;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int value);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));

    private static void SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value)
    {
        if (IntPtr.Size == 8)
            SetWindowLongPtr64(hWnd, nIndex, value);
        else
            SetWindowLong32(hWnd, nIndex, value.ToInt32());
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void void_objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool value);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void void_objc_msgSend_long(IntPtr receiver, IntPtr selector, long value);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libXext.so.6")]
    private static extern int XShapeCombineMask(IntPtr display, IntPtr window, int destKind, int xOff, int yOff, IntPtr mask, int op);
}
