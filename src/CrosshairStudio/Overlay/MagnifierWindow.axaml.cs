using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using CrosshairStudio.Domain;

namespace CrosshairStudio.Overlay;

public partial class MagnifierWindow : Window
{
    private HudWidget? _widget;
    private MonitorInfo? _monitor;
    private bool _editMode;
    private bool _dragging;
    private bool _resizing;
    private PixelPoint _pressScreen;
    private PixelPoint _origin;
    private int _startSize;
    private Action<HudWidget>? _moved;

    public MagnifierWindow()
    {
        InitializeComponent();
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
    }

    public void BindMoved(Action<HudWidget> moved) => _moved = moved;

    public void Apply(Bitmap? frame, int pixelSize, MonitorInfo monitor, PixelPoint place, HudWidget widget, bool editMode)
    {
        if (frame == null || pixelSize < 32)
        {
            Hide();
            return;
        }

        _widget = widget;
        _monitor = monitor;
        _editMode = editMode;
        if (!ReferenceEquals(Frame.Source, frame))
            Frame.Source = frame;
        Frame.InvalidateVisual();
        Grip.IsVisible = editMode;

        if (!_resizing)
        {
            var dip = pixelSize / Math.Max(0.5, monitor.Scaling);
            Width = dip;
            Height = dip;
        }

        if (!_dragging && !_resizing)
        {
            var size = (int)Math.Round(Width * monitor.Scaling);
            var x = Math.Clamp(place.X, monitor.X + 8, monitor.X + monitor.Width - size - 8);
            var y = Math.Clamp(place.Y, monitor.Y + 8, monitor.Y + monitor.Height - size - 8);
            Position = new PixelPoint(x, y);
        }

        Cursor = editMode ? new Cursor(StandardCursorType.SizeAll) : Cursor.Default;
        Topmost = true;
        if (!IsVisible)
            Show();
        NativeOverlay.Apply(this, clickThrough: !editMode, true);
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_editMode || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var point = e.GetPosition(this);
        _pressScreen = this.PointToScreen(point);
        _origin = Position;
        _startSize = (int)Math.Round(Math.Max(Width, Height) * (_monitor?.Scaling ?? 1));
        _resizing = point.X >= Bounds.Width - 22 && point.Y >= Bounds.Height - 22;
        _dragging = !_resizing;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_monitor == null)
            return;

        if (_resizing)
        {
            var now = this.PointToScreen(e.GetPosition(this));
            var delta = Math.Max(now.X - _pressScreen.X, now.Y - _pressScreen.Y);
            var size = Math.Clamp(_startSize + delta, 80, 720);
            var dip = size / Math.Max(0.5, _monitor.Scaling);
            Width = dip;
            Height = dip;
            e.Handled = true;
            return;
        }

        if (!_dragging)
            return;
        var moved = this.PointToScreen(e.GetPosition(this));
        Position = new PixelPoint(_origin.X + moved.X - _pressScreen.X, _origin.Y + moved.Y - _pressScreen.Y);
        e.Handled = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging && !_resizing)
            return;
        var resized = _resizing;
        _dragging = false;
        _resizing = false;
        e.Pointer.Capture(null);
        if (_widget != null && _monitor != null)
        {
            _widget.LensPinned = true;
            _widget.LensX = Position.X - _monitor.X;
            _widget.LensY = Position.Y - _monitor.Y;
            if (resized)
                _widget.OutputSize = (int)Math.Round(Math.Max(Width, Height) * _monitor.Scaling);
            _moved?.Invoke(_widget);
        }
        e.Handled = true;
    }
}
