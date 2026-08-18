using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CrosshairStudio.Domain;
using CrosshairStudio.Rendering;

namespace CrosshairStudio.Controls;

public sealed class CrosshairView : Control
{
    private Crosshair? _subscribed;
    private DispatcherTimer? _pulse;
    private double _pulsePhase;
    private CrosshairPart? _dragPart;
    private Point _dragOrigin;
    private double _dragStartX;
    private double _dragStartY;

    public static readonly StyledProperty<Crosshair?> CrosshairProperty =
        AvaloniaProperty.Register<CrosshairView, Crosshair?>(nameof(Crosshair));

    public static readonly StyledProperty<bool> UsePhysicalPixelsProperty =
        AvaloniaProperty.Register<CrosshairView, bool>(nameof(UsePhysicalPixels), true);

    public static readonly StyledProperty<double> MonitorScaleProperty =
        AvaloniaProperty.Register<CrosshairView, double>(nameof(MonitorScale), 1);

    public static readonly StyledProperty<double> PreviewScaleProperty =
        AvaloniaProperty.Register<CrosshairView, double>(nameof(PreviewScale), 1);

    public static readonly StyledProperty<bool> AllowPartDragProperty =
        AvaloniaProperty.Register<CrosshairView, bool>(nameof(AllowPartDrag));

    public static readonly StyledProperty<CrosshairPart?> SelectedPartProperty =
        AvaloniaProperty.Register<CrosshairView, CrosshairPart?>(nameof(SelectedPart), default, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    static CrosshairView()
    {
        AffectsRender<CrosshairView>(CrosshairProperty, UsePhysicalPixelsProperty, MonitorScaleProperty, PreviewScaleProperty, SelectedPartProperty);
        CrosshairProperty.Changed.AddClassHandler<CrosshairView>((v, e) => v.OnCrosshairChanged(e));
        AllowPartDragProperty.Changed.AddClassHandler<CrosshairView>((v, e) => v.IsHitTestVisible = e.GetNewValue<bool>());
    }

    public CrosshairView()
    {
        IsHitTestVisible = false;
        Focusable = false;
        ClipToBounds = true;
        Cursor = new Cursor(StandardCursorType.Arrow);
        DetachedFromVisualTree += (_, _) =>
        {
            StopPulse();
            Unhook(_subscribed);
        };
        AttachedToVisualTree += (_, _) => SyncPulse(Crosshair);
    }

    public Crosshair? Crosshair
    {
        get => GetValue(CrosshairProperty);
        set => SetValue(CrosshairProperty, value);
    }

    public bool UsePhysicalPixels
    {
        get => GetValue(UsePhysicalPixelsProperty);
        set => SetValue(UsePhysicalPixelsProperty, value);
    }

    public double MonitorScale
    {
        get => GetValue(MonitorScaleProperty);
        set => SetValue(MonitorScaleProperty, value);
    }

    public double PreviewScale
    {
        get => GetValue(PreviewScaleProperty);
        set => SetValue(PreviewScaleProperty, value);
    }

    public bool AllowPartDrag
    {
        get => GetValue(AllowPartDragProperty);
        set => SetValue(AllowPartDragProperty, value);
    }

    public CrosshairPart? SelectedPart
    {
        get => GetValue(SelectedPartProperty);
        set => SetValue(SelectedPartProperty, value);
    }
    public double UnitScale
    {
        get
        {
            var scale = MonitorScale <= 0 ? 1 : MonitorScale;
            var pixelScale = UsePhysicalPixels ? 1.0 / scale : 1.0;
            return pixelScale * PreviewScale;
        }
    }

    public override void Render(DrawingContext context)
    {
        var ch = Crosshair;
        if (ch == null)
            return;

        var scale = UnitScale;
        if (ch.EnablePulse)
            scale *= 1 + Math.Sin(_pulsePhase) * ch.PulseAmount;
        CrosshairPainter.Draw(context, ch, Bounds.Size, scale);
        DrawSelection(context, ch, scale);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!AllowPartDrag || Crosshair == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var hit = HitTestPart(e.GetPosition(this));
        if (hit != null)
            SelectedPart = hit;

        _dragPart = SelectedPart ?? hit;
        if (_dragPart == null)
            return;

        _dragOrigin = ToUnrotated(e.GetPosition(this));
        _dragStartX = _dragPart.OffsetX;
        _dragStartY = _dragPart.OffsetY;
        e.Pointer.Capture(this);
        e.Handled = true;
        Cursor = new Cursor(StandardCursorType.SizeAll);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!AllowPartDrag || Crosshair == null)
            return;

        if (_dragPart != null)
        {
            var now = ToUnrotated(e.GetPosition(this));
            var scale = Math.Max(0.01, UnitScale);
            _dragPart.OffsetX = Math.Clamp(_dragStartX + (now.X - _dragOrigin.X) / scale, -80, 80);
            _dragPart.OffsetY = Math.Clamp(_dragStartY + (now.Y - _dragOrigin.Y) / scale, -80, 80);
            e.Handled = true;
            return;
        }

        Cursor = new Cursor(HitTestPart(e.GetPosition(this)) != null ? StandardCursorType.SizeAll : StandardCursorType.Arrow);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragPart == null)
            return;
        _dragPart = null;
        e.Pointer.Capture(null);
        Cursor = new Cursor(StandardCursorType.Arrow);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _dragPart = null;
        Cursor = new Cursor(StandardCursorType.Arrow);
    }
    private void OnCrosshairChanged(AvaloniaPropertyChangedEventArgs e)
    {
        Unhook(_subscribed);
        _subscribed = e.NewValue as Crosshair;
        Hook(_subscribed);
        SyncPulse(_subscribed);
        InvalidateVisual();
    }

    private void Hook(Crosshair? ch)
    {
        if (ch == null)
            return;
        ch.PropertyChanged += OnModelChanged;
        ch.ExtraParts.CollectionChanged += OnPartsChanged;
        foreach (var part in ch.ExtraParts)
            part.PropertyChanged += OnModelChanged;
    }

    private void Unhook(Crosshair? ch)
    {
        if (ch == null)
            return;
        ch.PropertyChanged -= OnModelChanged;
        ch.ExtraParts.CollectionChanged -= OnPartsChanged;
        foreach (var part in ch.ExtraParts)
            part.PropertyChanged -= OnModelChanged;
    }

    private void OnPartsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CrosshairPart part in e.NewItems)
                part.PropertyChanged += OnModelChanged;
        }

        if (e.OldItems != null)
        {
            foreach (CrosshairPart part in e.OldItems)
                part.PropertyChanged -= OnModelChanged;
        }

        InvalidateVisual();
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Crosshair.EnablePulse) or nameof(Crosshair.PulseSpeed))
            SyncPulse(Crosshair);
        InvalidateVisual();
    }

    private void SyncPulse(Crosshair? ch)
    {
        if (ch is not { EnablePulse: true })
        {
            StopPulse();
            return;
        }

        _pulse ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / 24) };
        _pulse.Tick -= OnPulseTick;
        _pulse.Tick += OnPulseTick;
        if (!_pulse.IsEnabled)
            _pulse.Start();
    }

    private void OnPulseTick(object? sender, EventArgs e)
    {
        var speed = Math.Clamp(Crosshair?.PulseSpeed ?? 1.2, 0.2, 6);
        _pulsePhase += speed * (Math.PI * 2) / 24;
        if (_pulsePhase > Math.PI * 2)
            _pulsePhase -= Math.PI * 2;
        InvalidateVisual();
    }

    private void StopPulse()
    {
        if (_pulse == null)
            return;
        _pulse.Stop();
        _pulse.Tick -= OnPulseTick;
        _pulsePhase = 0;
    }

    private void DrawSelection(DrawingContext context, Crosshair ch, double scale)
    {
        if (!AllowPartDrag || SelectedPart is not { Enabled: true } part)
            return;

        var (cx, cy) = Center(ch, scale);
        var dx = part.OffsetX * scale;
        var dy = part.OffsetY * scale;
        var rad = ch.Rotation * Math.PI / 180.0;
        var point = new Point(
            cx + dx * Math.Cos(rad) - dy * Math.Sin(rad),
            cy + dx * Math.Sin(rad) + dy * Math.Cos(rad));
        var radius = Math.Max(10, part.Size * scale / 2 + 6);
        var pen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(150, 245, 245, 247)), 1.2)
        {
            DashStyle = DashStyle.Dash
        };
        context.DrawEllipse(null, pen, point, radius, radius);
    }

    private CrosshairPart? HitTestPart(Point pointer)
    {
        var ch = Crosshair;
        if (ch?.ExtraParts is not { Count: > 0 })
            return null;

        var scale = UnitScale;
        var (cx, cy) = Center(ch, scale);
        var local = ToUnrotated(pointer);
        CrosshairPart? best = null;
        var bestDist = double.MaxValue;
        foreach (var part in ch.ExtraParts)
        {
            if (!part.Enabled)
                continue;
            var px = cx + part.OffsetX * scale;
            var py = cy + part.OffsetY * scale;
            var dx = local.X - px;
            var dy = local.Y - py;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            var radius = Math.Max(14, part.Size * scale / 2 + 8);
            if (dist <= radius && dist < bestDist)
            {
                best = part;
                bestDist = dist;
            }
        }

        return best;
    }

    private Point ToUnrotated(Point pointer)
    {
        var ch = Crosshair;
        if (ch == null)
            return pointer;

        var scale = UnitScale;
        var (cx, cy) = Center(ch, scale);
        var dx = pointer.X - cx;
        var dy = pointer.Y - cy;
        var rad = -ch.Rotation * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return new Point(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
    }

    private (double cx, double cy) Center(Crosshair ch, double scale)
        => (Bounds.Width / 2 + ch.OffsetX * scale, Bounds.Height / 2 + ch.OffsetY * scale);
}