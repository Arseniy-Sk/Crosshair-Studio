using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace CrosshairStudio.Infrastructure;

public sealed class HotkeyHook : IDisposable
{
    private const uint ModNorepeat = 0x4000;
    private const int WmHotkey = 0x0312;
    private const int WmDestroy = 0x0002;
    private const int WmClose = 0x0010;
    private const int WmAppRebind = 0x8001;
    private const int HotkeyId = 1;

    private readonly WindowProc _proc;
    private readonly object _gate = new();
    private Thread? _thread;
    private IntPtr _hwnd;
    private Action? _callback;
    private uint _mods;
    private uint _vk;
    private string _className = "";
    private volatile bool _run;
    private bool _classRegistered;
    private bool _disposed;

    public bool IsRegistered { get; private set; }

    public HotkeyHook() => _proc = OnWindowMessage;

    public bool Start(uint modifiers, uint virtualKey, Action callback)
    {
        lock (_gate)
        {
            if (_disposed)
                return false;
            _callback = callback;
            _mods = modifiers | ModNorepeat;
            _vk = virtualKey;
        }

        if (!OperatingSystem.IsWindows() || virtualKey == 0)
        {
            Suspend();
            return false;
        }

        if (_thread is { IsAlive: true } && _hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WmAppRebind, IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        return Spawn();
    }

    public void Suspend()
    {
        lock (_gate)
            _vk = 0;
        var hwnd = _hwnd;
        if (hwnd != IntPtr.Zero)
            PostMessage(hwnd, WmAppRebind, IntPtr.Zero, IntPtr.Zero);
        IsRegistered = false;
    }

    public void Stop() => Suspend();

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _run = false;
            _callback = null;
        }

        var hwnd = _hwnd;
        if (hwnd != IntPtr.Zero)
            PostMessage(hwnd, WmClose, IntPtr.Zero, IntPtr.Zero);

        var thread = _thread;
        if (thread != null && thread.IsAlive)
            thread.Join(TimeSpan.FromSeconds(1));

        _thread = null;
        IsRegistered = false;
    }

    private bool Spawn()
    {
        using var ready = new ManualResetEventSlim(false);
        var ok = false;
        _run = true;
        _thread = new Thread(() =>
        {
            try
            {
                ok = CreateWindowAndRegister();
            }
            catch
            {
                ok = false;
            }
            finally
            {
                ready.Set();
            }

            if (_hwnd == IntPtr.Zero)
                return;

            while (_run && GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            CleanupWindow();
        })
        {
            IsBackground = true,
            Name = "CS-Hotkey"
        };
        if (OperatingSystem.IsWindows())
            _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        ready.Wait(TimeSpan.FromSeconds(2));
        IsRegistered = ok;
        return ok;
    }

    private bool CreateWindowAndRegister()
    {
        _className = "CrosshairStudioHotkey" + Environment.ProcessId;
        var hInstance = GetModuleHandle(null);
        var wnd = new WndClassEx
        {
            cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_proc),
            hInstance = hInstance,
            lpszClassName = _className
        };

        var atom = RegisterClassEx(ref wnd);
        _classRegistered = atom != 0 || Marshal.GetLastWin32Error() == 1410;
        _hwnd = CreateWindowEx(0, _className, "CrosshairStudioHotkey", 0, 0, 0, 0, 0,
            new IntPtr(-3), IntPtr.Zero, hInstance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
            return false;

        return ApplyHotkey();
    }

    private bool ApplyHotkey()
    {
        if (_hwnd == IntPtr.Zero)
            return false;

        UnregisterHotKey(_hwnd, HotkeyId);
        uint mods;
        uint vk;
        lock (_gate)
        {
            mods = _mods;
            vk = _vk;
        }

        if (vk == 0)
        {
            IsRegistered = false;
            return false;
        }

        IsRegistered = RegisterHotKey(_hwnd, HotkeyId, mods, vk);
        return IsRegistered;
    }

    private void CleanupWindow()
    {
        var hwnd = _hwnd;
        _hwnd = IntPtr.Zero;
        if (hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(hwnd, HotkeyId);
            DestroyWindow(hwnd);
        }

        if (_classRegistered && !string.IsNullOrEmpty(_className))
        {
            UnregisterClass(_className, GetModuleHandle(null));
            _classRegistered = false;
        }
    }

    private IntPtr OnWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (msg == WmAppRebind)
            {
                ApplyHotkey();
                return IntPtr.Zero;
            }

            if (msg == WmHotkey)
            {
                Action? callback;
                lock (_gate)
                    callback = _callback;
                if (callback != null)
                    Dispatcher.UIThread.Post(callback);
                return IntPtr.Zero;
            }

            if (msg == WmClose)
            {
                DestroyWindow(hWnd);
                return IntPtr.Zero;
            }

            if (msg == WmDestroy)
            {
                PostQuitMessage(0);
                return IntPtr.Zero;
            }
        }
        catch
        {
            // Keep the hook thread alive.
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private delegate IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WndClassEx lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
