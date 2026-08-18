using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CrosshairStudio.Domain;
using CrosshairStudio.Rendering;

namespace CrosshairStudio.Controls;

public sealed class HudView : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<HudView, string>(nameof(Text), "");

    public static readonly StyledProperty<uint> ColorProperty =
        AvaloniaProperty.Register<HudView, uint>(nameof(Color), 0xFFFFFFFF);

    public static readonly StyledProperty<double> ContentOpacityProperty =
        AvaloniaProperty.Register<HudView, double>(nameof(ContentOpacity), 1);

    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<HudView, double>(nameof(Scale), 1);

    public static readonly StyledProperty<WidgetChrome> ChromeProperty =
        AvaloniaProperty.Register<HudView, WidgetChrome>(nameof(Chrome));

    public static readonly StyledProperty<IImage?> IconProperty =
        AvaloniaProperty.Register<HudView, IImage?>(nameof(Icon));

    public static readonly StyledProperty<double> WrapWidthProperty =
        AvaloniaProperty.Register<HudView, double>(nameof(WrapWidth));

    public static readonly StyledProperty<double> WrapHeightProperty =
        AvaloniaProperty.Register<HudView, double>(nameof(WrapHeight));

    static HudView()
    {
        AffectsRender<HudView>(TextProperty, ColorProperty, ContentOpacityProperty, ScaleProperty, ChromeProperty, IconProperty, WrapWidthProperty, WrapHeightProperty);
        AffectsMeasure<HudView>(TextProperty, ScaleProperty, IconProperty, ChromeProperty, WrapWidthProperty, WrapHeightProperty);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public uint Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public double ContentOpacity
    {
        get => GetValue(ContentOpacityProperty);
        set => SetValue(ContentOpacityProperty, value);
    }

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public WidgetChrome Chrome
    {
        get => GetValue(ChromeProperty);
        set => SetValue(ChromeProperty, value);
    }

    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public double WrapWidth
    {
        get => GetValue(WrapWidthProperty);
        set => SetValue(WrapWidthProperty, value);
    }

    public double WrapHeight
    {
        get => GetValue(WrapHeightProperty);
        set => SetValue(WrapHeightProperty, value);
    }

    private double EffectiveScale => Math.Clamp(Scale <= 0 ? 1 : Scale, 0.5, 2.5);
    private bool IsSticky => Chrome == WidgetChrome.Sticky;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsSticky)
        {
            var w = WrapWidth > 0 ? WrapWidth : 220;
            var h = WrapHeight > 0 ? WrapHeight : 168;
            return new Size(w, h);
        }

        var ft = CreateText();
        var scale = EffectiveScale;
        var icon = IconSize;
        var gap = icon > 0 && HasText ? 8 * scale : 0;
        var padX = Chrome == WidgetChrome.Plain ? 8 * scale : 28 * scale;
        var padY = Chrome == WidgetChrome.Plain ? 6 * scale : 16 * scale;
        return new Size(
            Math.Ceiling(ft.Width) + icon + gap + padX,
            Math.Max(Math.Ceiling(ft.Height), icon) + padY);
    }

    public override void Render(DrawingContext context)
    {
        if (IsSticky)
        {
            DrawSticky(context);
            return;
        }

        var ft = CreateText();
        var scale = EffectiveScale;
        var radius = 14 * scale;
        var rect = new RoundedRect(new Rect(0.5, 0.5, Math.Max(1, Bounds.Width - 1), Math.Max(1, Bounds.Height - 1)), radius);
        DrawChrome(context, rect);

        var icon = IconSize;
        var gap = icon > 0 && HasText ? 8 * scale : 0;
        var contentWidth = ft.Width + icon + gap;
        var x = (Bounds.Width - contentWidth) / 2;
        var yText = (Bounds.Height - ft.Height) / 2;
        if (Icon != null && icon > 0)
        {
            var yIcon = (Bounds.Height - icon) / 2;
            context.DrawImage(Icon, new Rect(Math.Max(0, x), Math.Max(0, yIcon), icon, icon));
            x += icon + gap;
        }

        var origin = new Point(Math.Max(0, x), Math.Max(0, yText));
        var outline = new SolidColorBrush(Colors.Black) { Opacity = 0.55 * ContentOpacity };
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0)
                continue;
            context.DrawText(CreateText(outline), origin + new Point(dx, dy));
        }

        context.DrawText(ft, origin);
    }

    private void DrawSticky(DrawingContext context)
    {
        var paper = CrosshairPainter.ToColor(Color == 0 || Color == 0xFFFFFFFF ? 0xFFFFE566 : Color);
        paper = Avalonia.Media.Color.FromArgb((byte)Math.Clamp(255 * ContentOpacity, 40, 255), paper.R, paper.G, paper.B);
        var rect = new RoundedRect(new Rect(0.5, 0.5, Math.Max(1, Bounds.Width - 1), Math.Max(1, Bounds.Height - 1)), 4);
        context.DrawRectangle(new SolidColorBrush(paper), new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(40, 0, 0, 0)), 1), rect);
        var bar = new Rect(1, 1, Math.Max(1, Bounds.Width - 2), 22);
        context.FillRectangle(new SolidColorBrush(Avalonia.Media.Color.FromArgb(40, 0, 0, 0)), bar);

        var ink = new SolidColorBrush(Avalonia.Media.Color.FromArgb(220, 50, 42, 12)) { Opacity = ContentOpacity };
        var ft = CreateText(ink, wrap: true);
        context.DrawText(ft, new Point(10, 30));
    }

    private void DrawChrome(DrawingContext context, RoundedRect rect)
    {
        switch (Chrome)
        {
            case WidgetChrome.Plain:
                return;
            case WidgetChrome.Frame:
                context.DrawRectangle(new SolidColorBrush(Avalonia.Media.Color.FromArgb((byte)Math.Clamp(140 * ContentOpacity, 0, 255), 16, 16, 18)),
                    new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(90, 255, 255, 255)), 1), rect);
                return;
            default:
                context.DrawRectangle(new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0.2, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0.85, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Avalonia.Media.Color.FromArgb(80, 255, 255, 255), 0),
                        new GradientStop(Avalonia.Media.Color.FromArgb(130, 32, 32, 36), 0.4),
                        new GradientStop(Avalonia.Media.Color.FromArgb(155, 16, 16, 18), 1)
                    }
                }, null, rect);
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(70, 255, 255, 255)), 1), rect);
                break;
        }
    }

    private bool HasText => !string.IsNullOrWhiteSpace(Text);
    private double IconSize => Icon == null ? 0 : 18 * EffectiveScale;

    private FormattedText CreateText(IBrush? brush = null, bool wrap = false)
    {
        brush ??= new SolidColorBrush(CrosshairPainter.ToColor(Color)) { Opacity = ContentOpacity };
        var ft = new FormattedText(
            string.IsNullOrWhiteSpace(Text) ? " " : Text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI, Inter, SF Pro Text, Ubuntu, sans-serif"), FontStyle.Normal, wrap ? FontWeight.Normal : FontWeight.SemiBold),
            (wrap ? 14 : 15) * EffectiveScale,
            brush);
        if (wrap)
        {
            ft.MaxTextWidth = Math.Max(40, Bounds.Width > 1 ? Bounds.Width - 20 : (WrapWidth > 0 ? WrapWidth - 20 : 200));
            ft.MaxTextHeight = Math.Max(40, Bounds.Height > 1 ? Bounds.Height - 40 : (WrapHeight > 0 ? WrapHeight - 40 : 120));
        }

        return ft;
    }
}
