using Avalonia;
using Avalonia.Controls;
using CrosshairStudio.Domain;
using CrosshairStudio.Rendering;

namespace CrosshairStudio.Overlay;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void Apply(Crosshair crosshair, DisplaySettings settings, MonitorInfo monitor)
    {
        Visual.Crosshair = crosshair;
        Visual.UsePhysicalPixels = settings.UsePhysicalPixels;
        Visual.MonitorScale = monitor.Scaling;
        Visual.PreviewScale = 1;

        var extent = CrosshairPainter.EstimateExtent(crosshair);
        var padding = 32.0;
        var sizePx = settings.UsePhysicalPixels
            ? extent * 2 + padding
            : (extent * 2 + padding) * monitor.Scaling;
        sizePx = Math.Max(48, sizePx);

        var sizeDip = sizePx / Math.Max(0.5, monitor.Scaling);
        Width = sizeDip;
        Height = sizeDip;

        var x = monitor.CenterX - (int)(sizePx / 2) + settings.OffsetX;
        var y = monitor.CenterY - (int)(sizePx / 2) + settings.OffsetY;
        Position = new PixelPoint(x, y);

        Topmost = true;
        NativeOverlay.Apply(this, settings.ClickThrough, true);
    }
}
