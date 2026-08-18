using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CrosshairStudio.Infrastructure.Sampling;

public sealed class TelemetrySampler : IDisposable
{
    private readonly Process _process = Process.GetCurrentProcess();
    private TimeSpan _lastCpu;
    private long _lastStamp = Stopwatch.GetTimestamp();
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public double StudioCpu { get; private set; }
    public double RamUsedGb { get; private set; }
    public double RamTotalGb { get; private set; }
    public int? PingMs { get; private set; }

    public void Start(bool system, bool ping, string host)
    {
        Stop();
        if (!system && !ping)
            return;

        _lastCpu = _process.TotalProcessorTime;
        _lastStamp = Stopwatch.GetTimestamp();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _loop = Task.Run(() => RunAsync(system, ping, host, token), token);
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch
        {
            // Ignore teardown races.
        }

        _cts = null;
        _loop = null;
    }

    public string FormatSystem()
        => $"CPU {StudioCpu:0.0}%   RAM {RamUsedGb:0.0}/{RamTotalGb:0.0} GB";

    public string FormatPing()
        => PingMs is { } ms ? $"{ms} ms" : "…";

    public void Dispose() => Stop();

    private async Task RunAsync(bool system, bool ping, string host, CancellationToken token)
    {
        using var pingClient = ping ? new System.Net.NetworkInformation.Ping() : null;
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (system)
                    SampleSystem();
                if (pingClient != null)
                {
                    try
                    {
                        var reply = await pingClient.SendPingAsync(string.IsNullOrWhiteSpace(host) ? "1.1.1.1" : host, 800);
                        PingMs = reply.Status == System.Net.NetworkInformation.IPStatus.Success
                            ? (int)reply.RoundtripTime
                            : null;
                    }
                    catch
                    {
                        PingMs = null;
                    }
                }
            }
            catch
            {
                // Keep the sampler alive; next tick retries.
            }

            try
            {
                await Task.Delay(ping ? 3000 : 2000, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private void SampleSystem()
    {
        _process.Refresh();
        var cpu = _process.TotalProcessorTime;
        var now = Stopwatch.GetTimestamp();
        var wallMs = (now - _lastStamp) * 1000.0 / Stopwatch.Frequency;
        if (wallMs > 1)
        {
            var cpuMs = (cpu - _lastCpu).TotalMilliseconds;
            StudioCpu = Math.Clamp(cpuMs / (wallMs * Environment.ProcessorCount) * 100, 0, 100);
        }

        _lastCpu = cpu;
        _lastStamp = now;
        ReadMemory(out var used, out var total);
        RamUsedGb = used;
        RamTotalGb = total;
    }

    private static void ReadMemory(out double usedGb, out double totalGb)
    {
        usedGb = 0;
        totalGb = 0;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
                if (GlobalMemoryStatusEx(ref status) && status.TotalPhys > 0)
                {
                    totalGb = status.TotalPhys / 1073741824.0;
                    usedGb = (status.TotalPhys - status.AvailPhys) / 1073741824.0;
                    return;
                }
            }
            else if (OperatingSystem.IsLinux() && File.Exists("/proc/meminfo"))
            {
                long total = 0, available = 0;
                foreach (var line in File.ReadLines("/proc/meminfo"))
                {
                    if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                        total = ParseKb(line);
                    else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                        available = ParseKb(line);
                }

                if (total > 0)
                {
                    totalGb = total / 1048576.0;
                    usedGb = (total - available) / 1048576.0;
                    return;
                }
            }
        }
        catch
        {
            // Fall back to process working set.
        }

        usedGb = Process.GetCurrentProcess().WorkingSet64 / 1073741824.0;
        totalGb = Math.Max(usedGb, 1);
    }

    private static long ParseKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 && long.TryParse(parts[1], out var kb) ? kb : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
