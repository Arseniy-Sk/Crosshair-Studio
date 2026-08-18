using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace CrosshairStudio.Infrastructure.Sampling;

public sealed class HudStats
{
    private long _rx;
    private long _tx;
    private long _stamp = Stopwatch.GetTimestamp();
    private double _down;
    private double _up;

    public string FormatDate()
        => DateTime.Now.ToString("ddd d MMM", System.Globalization.CultureInfo.CurrentCulture);

    public string FormatUptime()
    {
        var span = TimeSpan.FromMilliseconds(Environment.TickCount64);
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes:00}m";
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes:00}m";
        return span.ToString(@"mm\:ss");
    }

    public string FormatBattery()
    {
        var (percent, charging, present) = ReadBattery();
        if (!present)
            return "AC";
        return charging ? $"BAT {percent}%  AC" : $"BAT {percent}%";
    }

    public string FormatNetwork()
    {
        SampleNetwork();
        return $"↓ {FormatRate(_down)}   ↑ {FormatRate(_up)}";
    }

    public string FormatDisk()
    {
        try
        {
            var root = OperatingSystem.IsWindows()
                ? Path.GetPathRoot(Environment.SystemDirectory)
                : "/";
            if (string.IsNullOrWhiteSpace(root))
                return "—";
            var drive = new DriveInfo(root);
            if (!drive.IsReady)
                return "—";
            var free = drive.AvailableFreeSpace / 1073741824.0;
            var label = drive.Name.TrimEnd('\\', '/');
            return $"{label}  {free:0.0} GB";
        }
        catch
        {
            return "—";
        }
    }

    public string FormatActiveWindow()
    {
        var title = ReadForegroundTitle();
        if (string.IsNullOrWhiteSpace(title))
            return "—";
        title = title.Trim();
        return title.Length <= 32 ? title : title[..31] + "…";
    }

    public string FormatDisplay(int width, int height, int scalePercent)
        => $"{width}×{height}  ·  {scalePercent}%";

    private void SampleNetwork()
    {
        long rx = 0, tx = 0;
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;
                var stats = nic.GetIPStatistics();
                rx += stats.BytesReceived;
                tx += stats.BytesSent;
            }
        }
        catch
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        var seconds = (now - _stamp) / (double)Stopwatch.Frequency;
        if (seconds > 0.2 && _stamp > 0)
        {
            _down = Math.Max(0, (rx - _rx) / seconds);
            _up = Math.Max(0, (tx - _tx) / seconds);
        }

        _rx = rx;
        _tx = tx;
        _stamp = now;
    }

    private static string FormatRate(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:0} B/s";
        if (bytesPerSecond < 1048576)
            return $"{bytesPerSecond / 1024:0.0} KB/s";
        return $"{bytesPerSecond / 1048576:0.00} MB/s";
    }

    private static (int percent, bool charging, bool present) ReadBattery()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (GetSystemPowerStatus(out var status))
                {
                    var present = status.BatteryFlag != 128 && status.BatteryLifePercent != 255;
                    var percent = present ? Math.Clamp((int)status.BatteryLifePercent, 0, 100) : 0;
                    var charging = status.ACLineStatus == 1;
                    return (percent, charging, present);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                foreach (var dir in new[] { "/sys/class/power_supply/BAT0", "/sys/class/power_supply/BAT1" })
                {
                    var cap = Path.Combine(dir, "capacity");
                    if (!File.Exists(cap))
                        continue;
                    var percent = int.TryParse(File.ReadAllText(cap).Trim(), out var p) ? p : 0;
                    var statusPath = Path.Combine(dir, "status");
                    var charging = File.Exists(statusPath) && File.ReadAllText(statusPath).Contains("Charging", StringComparison.OrdinalIgnoreCase);
                    return (percent, charging, true);
                }
            }
        }
        catch
        {
            // Desktops without a battery fall through.
        }

        return (0, true, false);
    }

    private static string ReadForegroundTitle()
    {
        if (!OperatingSystem.IsWindows())
            return "";
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return "";
            var length = GetWindowTextLength(hwnd);
            if (length <= 0)
                return "";
            var buffer = new StringBuilder(length + 1);
            return GetWindowText(hwnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : "";
        }
        catch
        {
            return "";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);
}
