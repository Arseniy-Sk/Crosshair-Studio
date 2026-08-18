using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CrosshairStudio.Domain;
using CrosshairStudio.Rendering;

namespace CrosshairStudio.Overlay;

public partial class HudWindow : Window
{
    private HudWidget? _widget;
    private MonitorInfo? _monitor;
    private bool _editMode;
    private bool _dragging;
    private PixelPoint _pressScreen;
    private PixelPoint _origin;
    private Action<HudWidget>? _moved;

    public HudWindow()
    {
        InitializeComponent();
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
        StickyEditor.LostFocus += (_, _) =>
        {
            if (_widget != null)
                _widget.Text = StickyEditor.Text ?? "";
        };
        StickyEditor.TextChanged += (_, _) =>
        {
            if (_widget != null && Sticky.IsVisible)
                _widget.Text = StickyEditor.Text ?? "";
        };
    }

    public bool IsEditing => _dragging || (_editMode && Sticky.IsVisible && StickyEditor.IsFocused);

    public void BindMoved(Action<HudWidget> moved) => _moved = moved;

    public void Apply(string text, HudWidget widget, DisplaySettings settings, MonitorInfo monitor, bool editMode)
    {
        _widget = widget;
        _monitor = monitor;
        _editMode = editMode;
        var sticky = widget.IsNotes;
        var paper = widget.Color == 0 || widget.Color == 0xFFFFFFFF ? 0xFFFFE566 : widget.Color;
        Sticky.IsVisible = sticky && editMode;
        Visual.IsVisible = !Sticky.IsVisible;
        Visual.Text = text;
        Visual.Color = sticky ? paper : widget.Color;
        Visual.ContentOpacity = widget.Opacity;
        Visual.Scale = widget.Scale <= 0 ? 1 : widget.Scale;
        Visual.Chrome = sticky ? WidgetChrome.Sticky : (widget.Kind == WidgetKind.Custom ? widget.Chrome : WidgetChrome.Glass);
        Visual.Icon = widget.Kind == WidgetKind.Custom && !sticky ? WidgetImages.Get(widget.ImagePath) : null;

        if (sticky)
        {
            var width = Math.Clamp(widget.NoteWidth <= 0 ? 220 : widget.NoteWidth, 120, 480);
            var height = Math.Clamp(widget.NoteHeight <= 0 ? 168 : widget.NoteHeight, 90, 420);
            Visual.WrapWidth = width;
            Visual.WrapHeight = height;
            Width = width;
            Height = height;
            Sticky.Background = new SolidColorBrush(CrosshairPainter.ToColor(paper));
            Sticky.Opacity = widget.Opacity;
            if (!StickyEditor.IsFocused)
                StickyEditor.Text = widget.Text ?? "";
            var line = (widget.Text ?? "").Split('\n')[0].Trim();
            StickyCaption.Text = string.IsNullOrWhiteSpace(line) ? "Note" : (line.Length > 22 ? line[..22] + "…" : line);
        }
        else
        {
            Visual.WrapWidth = 0;
            Visual.WrapHeight = 0;
            Visual.InvalidateMeasure();
            Visual.Measure(new Size(960, 160));
            var desired = Visual.DesiredSize;
            var widthPx = Math.Max(64, desired.Width * monitor.Scaling);
            var heightPx = Math.Max(28, desired.Height * monitor.Scaling);
            Width = widthPx / monitor.Scaling;
            Height = heightPx / monitor.Scaling;
        }

        if (!_dragging)
            Place(widget, monitor);

        Cursor = editMode
            ? (sticky ? Cursor.Default : new Cursor(StandardCursorType.SizeAll))
            : widget.NeedsClick
                ? new Cursor(StandardCursorType.Hand)
                : Cursor.Default;
        Topmost = true;
        NativeOverlay.Apply(this, clickThrough: !editMode && settings.ClickThrough && !widget.NeedsClick, true);
    }

    private void Place(HudWidget widget, MonitorInfo monitor)
    {
        var widthPx = Width * monitor.Scaling;
        var heightPx = Height * monitor.Scaling;
        if (widget.PinnedLayout)
        {
            Position = new PixelPoint(monitor.X + widget.LayoutX, monitor.Y + widget.LayoutY);
            return;
        }

        var (x, y) = AnchorPoint(widget.Anchor, monitor, (int)widthPx, (int)heightPx);
        Position = new PixelPoint(x + widget.OffsetX, y + widget.OffsetY);
    }

    private void StickyBar_OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_editMode || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        BeginDrag(e);
        e.Handled = true;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_editMode)
        {
            if (_widget != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                if (_widget.IsMagnifier && _widget.ZoomTrigger != ZoomTrigger.KeyHold)
                    _widget.Running = !_widget.Running;
                else
                    ApplyClick(_widget);
                e.Handled = true;
            }
            return;
        }

        if (_widget is { IsNotes: true })
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        BeginDrag(e);
        e.Handled = true;
    }

    private void BeginDrag(PointerPressedEventArgs e)
    {
        _dragging = true;
        _pressScreen = this.PointToScreen(e.GetPosition(this));
        _origin = Position;
        e.Pointer.Capture(this);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || _monitor == null)
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
            _widget.PinnedLayout = true;
            _widget.LayoutX = Position.X - _monitor.X;
            _widget.LayoutY = Position.Y - _monitor.Y;
            _moved?.Invoke(_widget);
        }
        e.Handled = true;
    }

    public static void ApplyClick(HudWidget widget)
    {
        switch (widget.ClickAction)
        {
            case WidgetClickAction.Increment:
                widget.Count++;
                break;
            case WidgetClickAction.Decrement:
                widget.Count = Math.Max(0, widget.Count - 1);
                break;
            case WidgetClickAction.Reset:
                widget.Count = 0;
                widget.ScoreLeft = 0;
                widget.ScoreRight = 0;
                widget.Running = false;
                break;
            case WidgetClickAction.Toggle:
                widget.Running = !widget.Running;
                break;
        }
    }

    private static (int x, int y) AnchorPoint(WidgetAnchor anchor, MonitorInfo monitor, int width, int height)
    {
        const int pad = 24;
        return anchor switch
        {
            WidgetAnchor.TopLeft => (monitor.X + pad, monitor.Y + pad),
            WidgetAnchor.TopRight => (monitor.X + monitor.Width - width - pad, monitor.Y + pad),
            WidgetAnchor.BottomLeft => (monitor.X + pad, monitor.Y + monitor.Height - height - pad),
            WidgetAnchor.BottomRight => (monitor.X + monitor.Width - width - pad, monitor.Y + monitor.Height - height - pad),
            WidgetAnchor.Top => (monitor.CenterX - width / 2, monitor.Y + pad),
            WidgetAnchor.Bottom => (monitor.CenterX - width / 2, monitor.Y + monitor.Height - height - pad),
            _ => (monitor.CenterX - width / 2, monitor.CenterY - height / 2)
        };
    }
}
