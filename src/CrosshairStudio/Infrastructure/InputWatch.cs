using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace CrosshairStudio.Infrastructure;

/// <summary>
/// Global key and extra-mouse-button watch. Does not swallow input.
/// </summary>
public sealed class InputWatch : IDisposable
{
    public event Action<int, bool>? Changed;

    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmXButtonDown = 0x020B;
    private const int WmXButtonUp = 0x020C;
    private const int VkXButton1 = 0x05;
    private const int VkXButton2 = 0x06;

    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private readonly HashSet<int> _held = [];
    private IntPtr _keyboard;
    private IntPtr _mouse;
    private bool _disposed;

    public bool IsActive => _keyboard != IntPtr.Zero || _mouse != IntPtr.Zero;

    public InputWatch()
    {
        _keyboardProc = OnKeyboard;
        _mouseProc = OnMouse;
    }

    public void Start()
    {
        if (!OperatingSystem.IsWindows() || _disposed || IsActive)
            return;

        var module = GetModuleHandle(null);
        _keyboard = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, module, 0);
        _mouse = SetWindowsHookEx(WhMouseLl, _mouseProc, module, 0);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_keyboard != IntPtr.Zero)
            UnhookWindowsHookEx(_keyboard);
        if (_mouse != IntPtr.Zero)
            UnhookWindowsHookEx(_mouse);
        _keyboard = IntPtr.Zero;
        _mouse = IntPtr.Zero;
        Changed = null;
    }

    public static bool IsModifierVk(int vk)
        => vk is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5;

    public static bool ShiftDown() => KeyDown(0x10);
    public static bool CtrlDown() => KeyDown(0x11);
    public static bool AltDown() => KeyDown(0x12);
    public static bool WinDown() => KeyDown(0x5B) || KeyDown(0x5C);

    private static bool KeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private IntPtr OnKeyboard(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var msg = wParam.ToInt32();
            var down = msg is WmKeyDown or WmSysKeyDown;
            var up = msg is WmKeyUp or WmSysKeyUp;
            if (down || up)
            {
                var info = Marshal.PtrToStructure<KbdLlHook>(lParam);
                Raise(info.VkCode, down);
            }
        }

        return CallNextHookEx(_keyboard, code, wParam, lParam);
    }

    private IntPtr OnMouse(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var msg = wParam.ToInt32();
            if (msg is WmXButtonDown or WmXButtonUp)
            {
                var info = Marshal.PtrToStructure<MsLlHook>(lParam);
                var which = (info.MouseData >> 16) & 0xFFFF;
                var vk = which == 2 ? VkXButton2 : VkXButton1;
                Raise(vk, msg == WmXButtonDown);
            }
        }

        return CallNextHookEx(_mouse, code, wParam, lParam);
    }

    private void Raise(int vk, bool down)
    {
        if (vk is 0x01 or 0x02 or 0x04)
            return;

        lock (_held)
        {
            if (down)
            {
                if (!_held.Add(vk))
                    return;
            }
            else
                _held.Remove(vk);
        }

        var handler = Changed;
        if (handler == null)
            return;
        Dispatcher.UIThread.Post(() => handler(vk, down));
    }

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHook
    {
        public int VkCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr Extra;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHook
    {
        public int X;
        public int Y;
        public int MouseData;
        public int Flags;
        public int Time;
        public IntPtr Extra;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
