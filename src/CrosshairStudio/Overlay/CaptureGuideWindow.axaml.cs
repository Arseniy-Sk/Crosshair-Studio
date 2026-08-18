using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CrosshairStudio.Domain;

namespace CrosshairStudio.Overlay;

public partial class CaptureGuideWindow : Window
{
    private HudWidget? _widget;
    private MonitorInfo? _monitor;
    private bool _dragging;
    private PixelPoint _pressScreen;
    private PixelPoint _origin;
    private Action<HudWidget>? _moved;

    public CaptureGuideWindow()
    {
        InitializeComponent();
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
    }

    public void BindMoved(Action<HudWidget> moved) => _moved = moved;

    public void Apply(HudWidget widget, MonitorInfo monitor, int pixelX, int pixelY, int pixelSize)
    {
        _widget = widget;
        _monitor = monitor;
        var dip = pixelSize / Math.Max(0.5, monitor.Scaling);
        Width = dip;
        Height = dip;
        if (!_dragging)
            Position = new PixelPoint(pixelX, pixelY);
        Cursor = new Cursor(StandardCursorType.SizeAll);
        Topmost = true;
        if (!IsVisible)
            Show();
        NativeOverlay.Apply(this, clickThrough: false, true);
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        _dragging = true;
        _pressScreen = this.PointToScreen(e.GetPosition(this));
        _origin = Position;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging)
            return;
        var now = this.PointToScreen(e.GetPosition(this));
        Position = new PixelPoint(_origin.X + now.X - _pressScreen.X, _origin.Y + now.Y - _pressScreen.Y);
        e.Handled = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging)
            return;
        _dragging = false;
        e.Pointer.Capture(null);
        if (_widget != null && _monitor != null)
        {
            _widget.ZoomSource = ZoomSource.Pinned;
            _widget.SourceX = Position.X - _monitor.X;
            _widget.SourceY = Position.Y - _monitor.Y;
            _moved?.Invoke(_widget);
        }
        e.Handled = true;
    }
}
