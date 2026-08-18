using Avalonia;
using Avalonia.Threading;
using CrosshairStudio.Domain;
using CrosshairStudio.Infrastructure.Sampling;

namespace CrosshairStudio.Overlay;

public sealed class OverlayService : IDisposable
{
    private readonly Dictionary<string, HudWindow> _hud = [];
    private readonly Dictionary<string, MagnifierWindow> _lenses = [];
    private readonly Dictionary<string, CaptureGuideWindow> _guides = [];
    private readonly ScreenGrabber _grabber = new();
    private readonly TelemetrySampler _telemetry = new();
    private readonly HudStats _stats = new();
    private readonly Dictionary<string, DateTime> _countdownEnds = [];
    private readonly Dictionary<string, TimeSpan> _stopwatch = [];
    private OverlayWindow? _aim;
    private DispatcherTimer? _clock;
    private DispatcherTimer? _zoom;
    private DispatcherTimer? _stack;
    private Crosshair? _crosshair;
    private IReadOnlyList<HudWidget> _widgets = [];
    private DisplaySettings? _settings;
    private MonitorInfo? _monitor;
    private bool _visible;
    private bool _showCrosshair = true;
    private bool _editMode;
    private DateTime _sessionStart;
    private bool _samplerOn;
    private Action<HudWidget>? _widgetMoved;

    public bool IsVisible => _visible;
    public bool IsEditMode => _editMode;

    public void BindWidgetMoved(Action<HudWidget> moved) => _widgetMoved = moved;

    public void SetEditMode(bool edit)
    {
        _editMode = edit;
        if (_visible)
            Dispatcher.UIThread.Post(ApplyNow);
    }

    public void Show(Crosshair crosshair, IReadOnlyList<HudWidget> widgets, DisplaySettings settings, MonitorInfo monitor, bool showCrosshair)
    {
        _crosshair = crosshair;
        _widgets = widgets;
        _settings = settings;
        _monitor = monitor;
        _showCrosshair = showCrosshair;
        if (!_visible)
            _sessionStart = DateTime.UtcNow;
        _visible = true;
        OverlayGuard.Arm();
        EnsureStack();
        Dispatcher.UIThread.Post(ApplyNow);
    }

    public void Hide()
    {
        _visible = false;
        _editMode = false;
        Dispatcher.UIThread.Post(() =>
        {
            _aim?.Hide();
            foreach (var window in _hud.Values)
                window.Hide();
            foreach (var lens in _lenses.Values)
                lens.Hide();
            foreach (var guide in _guides.Values)
                guide.Hide();
            StopClock();
            StopZoom();
            StopStack();
            OverlayGuard.Disarm();
            _telemetry.Stop();
            _samplerOn = false;
        });
    }

    public void Dispose()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _aim?.Close();
            if (_aim != null)
                NativeOverlay.Forget(_aim);
            _aim = null;
            foreach (var window in _hud.Values)
            {
                NativeOverlay.Forget(window);
                window.Close();
            }
            _hud.Clear();
            foreach (var lens in _lenses.Values)
            {
                NativeOverlay.Forget(lens);
                lens.Close();
            }
            _lenses.Clear();
            foreach (var guide in _guides.Values)
            {
                NativeOverlay.Forget(guide);
                guide.Close();
            }
            _guides.Clear();
            StopClock();
            StopZoom();
            StopStack();
            OverlayGuard.Disarm();
            _telemetry.Dispose();
            _grabber.Dispose();
        });
    }

    private void ApplyNow()
    {
        if (!_visible || _settings == null || _monitor == null || _crosshair == null)
            return;

        if (_showCrosshair)
        {
            _aim ??= new OverlayWindow();
            _aim.Apply(_crosshair, _settings, _monitor);
            if (!_aim.IsVisible)
                _aim.Show();
            NativeOverlay.Apply(_aim, _settings.ClickThrough, true);
        }
        else
        {
            _aim?.Hide();
        }

        SyncHudWindows();
        SyncTimers();
        RefreshHudTexts();
        RefreshMagnifiers();
    }

    private void SyncHudWindows()
    {
        var enabled = _widgets.Where(w => w.Enabled).Select(w => w.Id).ToHashSet();
        foreach (var id in _hud.Keys.ToArray())
        {
            if (enabled.Contains(id))
                continue;
            NativeOverlay.Forget(_hud[id]);
            _hud[id].Hide();
            _hud[id].Close();
            _hud.Remove(id);
        }

        foreach (var id in _lenses.Keys.ToArray())
        {
            var widget = _widgets.FirstOrDefault(w => w.Id == id);
            if (widget is { Enabled: true, IsMagnifier: true })
                continue;
            NativeOverlay.Forget(_lenses[id]);
            _lenses[id].Hide();
            _lenses[id].Close();
            _lenses.Remove(id);
        }

        foreach (var id in _guides.Keys.ToArray())
        {
            var widget = _widgets.FirstOrDefault(w => w.Id == id);
            if (widget is { Enabled: true, IsMagnifier: true } && _editMode)
                continue;
            NativeOverlay.Forget(_guides[id]);
            _guides[id].Hide();
            _guides[id].Close();
            _guides.Remove(id);
        }

        foreach (var widget in _widgets)
        {
            if (!widget.Enabled)
                continue;
            if (!_hud.ContainsKey(widget.Id))
            {
                var window = new HudWindow();
                window.BindMoved(OnHudMoved);
                _hud[widget.Id] = window;
                window.Show();
            }
        }

        NativeOverlay.RaiseAll();
    }

    private void SyncTimers()
    {
        var needClock = _widgets.Any(w => w.Enabled && NeedsClock(w));
        var needSystem = _widgets.Any(w => w.Enabled && NeedsSystem(w));
        var ping = _widgets.FirstOrDefault(w => w.Enabled && (w.EffectiveKind == WidgetKind.Ping || (w.IsTemplate && WidgetTemplate.UsesPing(w.Text))));
        var needZoom = _widgets.Any(w => w.Enabled && w.IsMagnifier && (w.Running || _editMode));

        if (needClock)
            EnsureClock();
        else
            StopClock();

        var samplerNeeded = needSystem || ping != null;
        if (samplerNeeded && !_samplerOn)
        {
            _telemetry.Start(needSystem, ping != null, ping?.Host ?? "1.1.1.1");
            _samplerOn = true;
        }
        else if (!samplerNeeded && _samplerOn)
        {
            _telemetry.Stop();
            _samplerOn = false;
        }

        if (needZoom)
            EnsureZoom();
        else
            StopZoom();

        EnsureStack();
    }

    private static bool NeedsClock(HudWidget widget)
        => widget.IsTemplate || widget.EffectiveKind is WidgetKind.Clock or WidgetKind.Session
            or WidgetKind.Stopwatch or WidgetKind.Countdown or WidgetKind.Date or WidgetKind.Battery
            or WidgetKind.Network or WidgetKind.Uptime or WidgetKind.ActiveApp or WidgetKind.Disk
            or WidgetKind.Display;

    private static bool NeedsSystem(HudWidget widget)
        => widget.EffectiveKind is WidgetKind.System or WidgetKind.Cpu or WidgetKind.Ram
           || (widget.IsTemplate && WidgetTemplate.UsesSystem(widget.Text));

    private void EnsureClock()
    {
        _clock ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick -= OnClockTick;
        _clock.Tick += OnClockTick;
        if (!_clock.IsEnabled)
            _clock.Start();
    }

    private void StopClock()
    {
        if (_clock == null)
            return;
        _clock.Stop();
        _clock.Tick -= OnClockTick;
    }

    private void EnsureZoom()
    {
        _zoom ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _zoom.Tick -= OnZoomTick;
        _zoom.Tick += OnZoomTick;
        if (!_zoom.IsEnabled)
            _zoom.Start();
    }

    private void StopZoom()
    {
        if (_zoom == null)
            return;
        _zoom.Stop();
        _zoom.Tick -= OnZoomTick;
    }

    private void EnsureStack()
    {
        _stack ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _stack.Tick -= OnStackTick;
        _stack.Tick += OnStackTick;
        if (!_stack.IsEnabled)
            _stack.Start();
    }

    private void StopStack()
    {
        if (_stack == null)
            return;
        _stack.Stop();
        _stack.Tick -= OnStackTick;
    }

    private void OnStackTick(object? sender, EventArgs e) => NativeOverlay.RaiseAll();

    private void OnZoomTick(object? sender, EventArgs e) => RefreshMagnifiers();

    private void OnClockTick(object? sender, EventArgs e) => RefreshHudTexts();

    private void RefreshHudTexts()
    {
        if (!_visible || _settings == null || _monitor == null)
            return;

        foreach (var widget in _widgets)
        {
            if (!widget.Enabled || !_hud.TryGetValue(widget.Id, out var window))
                continue;
            if (window.IsEditing)
                continue;
            window.Apply(Format(widget), widget, _settings, _monitor, _editMode);
        }
    }

    private void RefreshMagnifiers()
    {
        if (!_visible || _settings == null || _monitor == null)
            return;

        foreach (var widget in _widgets)
        {
            if (!widget.Enabled || !widget.IsMagnifier)
                continue;

            var capture = Math.Clamp(widget.CaptureSize <= 0 ? 88 : widget.CaptureSize, 32, 400);
            var (x, y) = CaptureOrigin(widget, capture);
            SyncGuide(widget, x, y, capture);

            if (!widget.Running && !_editMode)
            {
                if (_lenses.TryGetValue(widget.Id, out var idle))
                    idle.Hide();
                continue;
            }

            var frame = _grabber.Capture(x, y, capture, capture);
            if (!_lenses.TryGetValue(widget.Id, out var lens))
            {
                lens = new MagnifierWindow();
                lens.BindMoved(OnHudMoved);
                _lenses[widget.Id] = lens;
            }

            var zoom = Math.Clamp(widget.Zoom <= 0 ? 3 : widget.Zoom, 1.5, 8);
            var output = widget.OutputSize > 0
                ? Math.Clamp(widget.OutputSize, 80, 720)
                : (int)Math.Round(capture * zoom);
            var place = widget.LensPinned
                ? new PixelPoint(_monitor.X + widget.LensX, _monitor.Y + widget.LensY)
                : _hud.TryGetValue(widget.Id, out var chip)
                    ? new PixelPoint(chip.Position.X, chip.Position.Y + (int)(chip.Bounds.Height * _monitor.Scaling))
                    : new PixelPoint(_monitor.X + 24, _monitor.Y + 24);
            lens.Apply(frame, output, _monitor, place, widget, _editMode);
        }
    }

    private (int x, int y) CaptureOrigin(HudWidget widget, int capture)
    {
        var source = widget.EffectiveZoomSource;
        if (source == ZoomSource.Pinned)
            return (_monitor!.X + widget.SourceX, _monitor.Y + widget.SourceY);
        if (source == ZoomSource.Cursor)
        {
            var (cx, cy) = ScreenGrabber.CursorPosition();
            return (cx - capture / 2, cy - capture / 2);
        }

        return (_monitor!.CenterX + _settings!.OffsetX + widget.CaptureOffsetX - capture / 2,
            _monitor.CenterY + _settings.OffsetY + widget.CaptureOffsetY - capture / 2);
    }

    private void SyncGuide(HudWidget widget, int x, int y, int capture)
    {
        if (!_editMode)
        {
            if (_guides.TryGetValue(widget.Id, out var hidden))
                hidden.Hide();
            return;
        }

        if (!_guides.TryGetValue(widget.Id, out var guide))
        {
            guide = new CaptureGuideWindow();
            guide.BindMoved(OnHudMoved);
            _guides[widget.Id] = guide;
        }

        guide.Apply(widget, _monitor!, x, y, capture);
    }

    private string Format(HudWidget widget)
    {
        switch (widget.EffectiveKind)
        {
            case WidgetKind.Clock:
                return DateTime.Now.ToString("HH:mm:ss");
            case WidgetKind.Date:
                return _stats.FormatDate();
            case WidgetKind.Session:
                return FormatSpan(DateTime.UtcNow - _sessionStart);
            case WidgetKind.Stopwatch:
                return FormatSpan(StopwatchValue(widget));
            case WidgetKind.Countdown:
                return FormatSpan(CountdownValue(widget));
            case WidgetKind.System:
                return _telemetry.FormatSystem();
            case WidgetKind.Cpu:
                return $"CPU {_telemetry.StudioCpu:0.0}%";
            case WidgetKind.Ram:
                return $"RAM {_telemetry.RamUsedGb:0.0}/{_telemetry.RamTotalGb:0.0} GB";
            case WidgetKind.Ping:
                return _telemetry.FormatPing();
            case WidgetKind.Battery:
                return _stats.FormatBattery();
            case WidgetKind.Network:
                return _stats.FormatNetwork();
            case WidgetKind.Uptime:
                return _stats.FormatUptime();
            case WidgetKind.Disk:
                return _stats.FormatDisk();
            case WidgetKind.Display:
                return _monitor == null
                    ? "—"
                    : _stats.FormatDisplay(_monitor.Width, _monitor.Height, _monitor.ScalePercent);
            case WidgetKind.ActiveApp:
                return _stats.FormatActiveWindow();
            case WidgetKind.Counter:
            {
                var label = string.IsNullOrWhiteSpace(widget.Text) ? "" : widget.Text.Trim() + "  ";
                return label + widget.Count;
            }
            case WidgetKind.Score:
                return $"{widget.ScoreLeft}  —  {widget.ScoreRight}";
            case WidgetKind.Notes:
                return string.IsNullOrWhiteSpace(widget.Text) ? " " : widget.Text;
            case WidgetKind.Magnifier:
            {
                var zoom = widget.Zoom <= 0 ? 3 : widget.Zoom;
                return widget.Running ? $"×{zoom:0.#}  ●" : $"×{zoom:0.#}";
            }
            case WidgetKind.Template:
                return WidgetTemplate.Render(widget, _telemetry, _stats, DateTime.UtcNow - _sessionStart);
            default:
                return string.IsNullOrWhiteSpace(widget.Text) ? " " : widget.Text;
        }
    }

    private TimeSpan StopwatchValue(HudWidget widget)
    {
        if (!_stopwatch.TryGetValue(widget.Id, out var elapsed))
            elapsed = TimeSpan.Zero;
        if (widget.Running)
        {
            if (!_countdownEnds.ContainsKey("sw-" + widget.Id))
                _countdownEnds["sw-" + widget.Id] = DateTime.UtcNow;
            elapsed += DateTime.UtcNow - _countdownEnds["sw-" + widget.Id];
            _countdownEnds["sw-" + widget.Id] = DateTime.UtcNow;
            _stopwatch[widget.Id] = elapsed;
        }
        else
        {
            _countdownEnds.Remove("sw-" + widget.Id);
        }

        return elapsed;
    }

    private TimeSpan CountdownValue(HudWidget widget)
    {
        var key = "cd-" + widget.Id;
        if (widget.Running)
        {
            if (!_countdownEnds.ContainsKey(key))
                _countdownEnds[key] = DateTime.UtcNow.AddSeconds(Math.Max(1, widget.CountdownSeconds));
            var left = _countdownEnds[key] - DateTime.UtcNow;
            if (left <= TimeSpan.Zero)
            {
                widget.Running = false;
                _countdownEnds.Remove(key);
                return TimeSpan.Zero;
            }

            return left;
        }

        _countdownEnds.Remove(key);
        return TimeSpan.FromSeconds(Math.Max(0, widget.CountdownSeconds));
    }

    private void OnHudMoved(HudWidget widget) => _widgetMoved?.Invoke(widget);

    public void ResetStopwatch(string id)
    {
        _stopwatch[id] = TimeSpan.Zero;
        _countdownEnds.Remove("sw-" + id);
        if (_visible)
            Dispatcher.UIThread.Post(RefreshHudTexts);
    }

    private static string FormatSpan(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        return span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss") : span.ToString(@"mm\:ss");
    }
}
