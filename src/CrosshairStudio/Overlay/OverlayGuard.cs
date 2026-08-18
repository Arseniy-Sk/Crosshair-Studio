using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace CrosshairStudio.Overlay;

internal static class OverlayGuard
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutOfContext = 0;

    private static readonly WinEventProc Proc = OnEvent;
    private static IntPtr _hook;
    private static bool _armed;

    public static void Arm()
    {
        if (!OperatingSystem.IsWindows() || _armed)
            return;
        _hook = SetWinEventHook(EventSystemForeground, EventSystemForeground, IntPtr.Zero, Proc, 0, 0, WineventOutOfContext);
        _armed = _hook != IntPtr.Zero;
    }

    public static void Disarm()
    {
        if (_hook != IntPtr.Zero)
            UnhookWinEvent(_hook);
        _hook = IntPtr.Zero;
        _armed = false;
    }

    private static void OnEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        if (eventType != EventSystemForeground)
            return;
        Dispatcher.UIThread.Post(NativeOverlay.RaiseAll);
    }

    private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
}
