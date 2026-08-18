using Avalonia;
using Avalonia.Media;
using CrosshairStudio.Domain;

namespace CrosshairStudio.Rendering;

public static class CrosshairPainter
{
    public static double EstimateExtent(Crosshair ch)
    {
        var radius = Math.Max(ch.AxisWidth, ch.AxisHeight) / 2.0;
        radius = Math.Max(radius, ch.LineLength + ch.Gap);
        radius = Math.Max(radius, ch.InnerCircleSize / 2.0);
        radius = Math.Max(radius, ch.OuterRingSize / 2.0);
        radius = Math.Max(radius, ch.DotSize);
        radius += Math.Max(ch.Thickness, ch.LineThickness);
        if (ch.ShowTicks)
            radius += ch.TickLength + ch.TickSpacing * Math.Max(1, ch.TickCount);
        if (ch.ShowOutline)
            radius += ch.OutlineThickness * 2;
        if (ch.ShowGlow)
            radius += ch.GlowAmount;
        radius += Math.Max(Math.Abs(ch.OffsetX), Math.Abs(ch.OffsetY));
        if (ch.ExtraParts is { Count: > 0 })
        {
            foreach (var part in ch.ExtraParts)
            {
                if (!part.Enabled)
                    continue;
                radius = Math.Max(radius, part.Size / 2 + part.Thickness + part.Gap + Math.Max(Math.Abs(part.OffsetX), Math.Abs(part.OffsetY)));
            }
        }

        var rad = ch.Rotation * Math.PI / 180.0;
        var bound = radius * (Math.Abs(Math.Cos(rad)) + Math.Abs(Math.Sin(rad)));
        if (ch.EnablePulse)
            bound *= 1.0 + ch.PulseAmount;
        return Math.Max(24, bound);
    }

    public static void Draw(DrawingContext context, Crosshair ch, Size size, double unitScale)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return;

        var cx = size.Width / 2 + ch.OffsetX * unitScale;
        var cy = size.Height / 2 + ch.OffsetY * unitScale;
        var radians = ch.Rotation * Math.PI / 180.0;
        var matrix = Matrix.CreateTranslation(-cx, -cy)
                     * Matrix.CreateRotation(radians)
                     * Matrix.CreateTranslation(cx, cy);

        using (context.PushTransform(matrix))
            DrawUnrotated(context, ch, cx, cy, unitScale);
    }

    private static void DrawUnrotated(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        switch (ch.Type)
        {
            case CrosshairType.Cross:
                DrawCross(dc, ch, cx, cy, scale, false);
                break;
            case CrosshairType.CrossX:
                DrawCross(dc, ch, cx, cy, scale, true);
                break;
            case CrosshairType.Circle:
                DrawEllipse(dc, ch, cx, cy, scale);
                break;
            case CrosshairType.Dot:
                DrawDot(dc, ch, cx, cy, Math.Max(2, ch.Size / 4) * scale, ch.Color, ch.DotShape, scale);
                break;
            case CrosshairType.Square:
                DrawBox(dc, ch, cx, cy, scale, false);
                break;
            case CrosshairType.Diamond:
                DrawBox(dc, ch, cx, cy, scale, true);
                break;
            case CrosshairType.Triangle:
                DrawPolygon(dc, ch, cx, cy, scale, 3, -90);
                break;
            case CrosshairType.Hexagon:
                DrawPolygon(dc, ch, cx, cy, scale, 6, -90);
                break;
            case CrosshairType.Star:
                DrawStar(dc, ch, cx, cy, scale);
                break;
            case CrosshairType.Arc:
                DrawArc(dc, ch, cx, cy, scale);
                break;
            case CrosshairType.Brackets:
                DrawBrackets(dc, ch, cx, cy, scale);
                break;
            case CrosshairType.Chevron:
                DrawChevrons(dc, ch, cx, cy, scale);
                break;
            case CrosshairType.Custom:
                if (ch.ShowCrosshairLines)
                    DrawCustomLines(dc, ch, cx, cy, scale);
                break;
        }

        if (ch.ShowSideBrackets && ch.Type is not CrosshairType.Brackets)
            DrawBrackets(dc, ch, cx, cy, scale);

        if (ch.ShowOuterRing)
            DrawRing(dc, ch, cx, cy, ch.OuterRingSize * scale / 2, ch.OuterRingThickness * scale, ch.OuterRingColor, scale);

        if (ch.ShowInnerCircle)
            DrawRing(dc, ch, cx, cy, ch.InnerCircleSize * scale / 2, ch.InnerCircleThickness * scale, ch.InnerCircleColor, scale);

        if (ch.ShowCenterDot && ch.Type != CrosshairType.Dot)
            DrawDot(dc, ch, cx, cy, Math.Max(1, ch.DotSize) * scale, ch.DotColor, ch.DotShape, scale);

        DrawExtraParts(dc, ch, cx, cy, scale);
    }

    private static void DrawCross(DrawingContext dc, Crosshair ch, double cx, double cy, double scale, bool diagonal)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var halfW = ch.AxisWidth * scale / 2;
        var halfH = ch.AxisHeight * scale / 2;
        var gapX = ch.GapX * scale;
        var gapY = ch.GapY * scale;

        if (diagonal)
        {
            var len = Math.Max(halfW, halfH);
            var gap = Math.Max(gapX, gapY);
            DrawGappedDiagonal(dc, cx, cy, len, gap, 45, pens, ch.ShowTopLine || ch.ShowRightLine, ch.ShowBottomLine || ch.ShowLeftLine);
            DrawGappedDiagonal(dc, cx, cy, len, gap, 135, pens, ch.ShowTopLine || ch.ShowLeftLine, ch.ShowBottomLine || ch.ShowRightLine);
            if (ch.ShowTicks) DrawDiagonalTicks(dc, ch, cx, cy, len, gap, scale, pens);
            if (ch.ShowMidDots) DrawDiagonalMidDots(dc, ch, cx, cy, len, gap, scale);
            return;
        }

        if (ch.ShowRightLine) Segment(dc, new Point(cx + gapX, cy), new Point(cx + halfW, cy), pens);
        if (ch.ShowLeftLine) Segment(dc, new Point(cx - gapX, cy), new Point(cx - halfW, cy), pens);
        if (ch.ShowBottomLine) Segment(dc, new Point(cx, cy + gapY), new Point(cx, cy + halfH), pens);
        if (ch.ShowTopLine) Segment(dc, new Point(cx, cy - gapY), new Point(cx, cy - halfH), pens);
        if (ch.ShowTicks) DrawAxisTicks(dc, ch, cx, cy, halfW, halfH, gapX, gapY, scale, pens);
        if (ch.ShowMidDots) DrawAxisMidDots(dc, ch, cx, cy, halfW, halfH, gapX, gapY, scale);
    }

    private static void DrawCustomLines(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        var pens = Stroke(ch, ch.LineColor, ch.LineThickness * scale, scale);
        var length = ch.LineLength * scale;
        var gapX = ch.GapX * scale;
        var gapY = ch.GapY * scale;
        if (ch.ShowTopLine) Segment(dc, new Point(cx, cy - gapY - length), new Point(cx, cy - gapY), pens);
        if (ch.ShowBottomLine) Segment(dc, new Point(cx, cy + gapY), new Point(cx, cy + gapY + length), pens);
        if (ch.ShowLeftLine) Segment(dc, new Point(cx - gapX - length, cy), new Point(cx - gapX, cy), pens);
        if (ch.ShowRightLine) Segment(dc, new Point(cx + gapX, cy), new Point(cx + gapX + length, cy), pens);
    }

    private static void DrawGappedDiagonal(DrawingContext dc, double cx, double cy, double length, double gap, double angle, StrokeSet pens, bool positive, bool negative)
    {
        var rad = angle * Math.PI / 180;
        var dx = Math.Cos(rad);
        var dy = Math.Sin(rad);
        var origin = new Point(cx, cy);
        if (positive) Segment(dc, Offset(origin, dx, dy, gap), Offset(origin, dx, dy, length), pens);
        if (negative) Segment(dc, Offset(origin, -dx, -dy, gap), Offset(origin, -dx, -dy, length), pens);
    }

    private static void DrawEllipse(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var rx = ch.AxisWidth * scale / 2;
        var ry = ch.AxisHeight * scale / 2;
        var sweep = Math.Clamp(ch.ArcDegrees, 8, 360);
        if (sweep >= 359)
        {
            var fill = ch.FillShape ? Fill(ch, ch.Color) : null;
            PaintEllipse(dc, new Point(cx, cy), rx, ry, pens, fill);
            return;
        }

        DrawArc(dc, ch, cx, cy, scale);
    }

    private static void DrawArc(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var rx = ch.AxisWidth * scale / 2;
        var ry = ch.AxisHeight * scale / 2;
        var sweep = Math.Clamp(ch.ArcDegrees, 8, 360);
        const int steps = 48;
        var start = -90.0;
        Point? prev = null;
        for (var i = 0; i <= steps; i++)
        {
            var t = start + sweep * i / steps;
            var rad = t * Math.PI / 180;
            var point = new Point(cx + rx * Math.Cos(rad), cy + ry * Math.Sin(rad));
            if (prev != null)
                Segment(dc, prev.Value, point, pens);
            prev = point;
        }
    }

    private static void DrawStar(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var outer = Math.Max(ch.AxisWidth, ch.AxisHeight) * scale / 2;
        var inner = Math.Max(1, ch.GapX) * scale;
        for (var i = 0; i < 8; i++)
        {
            var rad = i * Math.PI / 4;
            var dx = Math.Cos(rad);
            var dy = Math.Sin(rad);
            Segment(dc, new Point(cx + dx * inner, cy + dy * inner), new Point(cx + dx * outer, cy + dy * outer), pens);
        }
    }

    private static void DrawBox(DrawingContext dc, Crosshair ch, double cx, double cy, double scale, bool diamond)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var w = ch.AxisWidth * scale;
        var h = ch.AxisHeight * scale;
        var fill = ch.FillShape ? Fill(ch, ch.Color) : null;
        if (diamond)
        {
            var geo = Polygon(new[]
            {
                new Point(cx, cy - h / 2),
                new Point(cx + w / 2, cy),
                new Point(cx, cy + h / 2),
                new Point(cx - w / 2, cy)
            }, fill != null);
            PaintGeometry(dc, geo, pens, fill);
            return;
        }

        var rect = new Rect(cx - w / 2, cy - h / 2, w, h);
        var radius = Math.Clamp(ch.CornerRadius * scale, 0, Math.Min(w, h) / 2);
        if (radius > 0.5)
        {
            var rounded = new RoundedRect(rect, radius, radius);
            if (pens.Glow != null) dc.DrawRectangle(null, pens.Glow, rounded);
            if (pens.Outline != null) dc.DrawRectangle(null, pens.Outline, rounded);
            dc.DrawRectangle(fill, pens.Main, rounded);
            return;
        }

        if (pens.Glow != null) dc.DrawRectangle(null, pens.Glow, rect);
        if (pens.Outline != null) dc.DrawRectangle(null, pens.Outline, rect);
        dc.DrawRectangle(fill, pens.Main, rect);
    }

    private static void DrawAxisTicks(DrawingContext dc, Crosshair ch, double cx, double cy, double halfW, double halfH, double gapX, double gapY, double scale, StrokeSet pens)
    {
        TickArm(dc, ch, cx, cy, 1, 0, halfW, gapX, scale, pens, ch.ShowRightLine);
        TickArm(dc, ch, cx, cy, -1, 0, halfW, gapX, scale, pens, ch.ShowLeftLine);
        TickArm(dc, ch, cx, cy, 0, 1, halfH, gapY, scale, pens, ch.ShowBottomLine);
        TickArm(dc, ch, cx, cy, 0, -1, halfH, gapY, scale, pens, ch.ShowTopLine);
    }

    private static void DrawDiagonalTicks(DrawingContext dc, Crosshair ch, double cx, double cy, double length, double gap, double scale, StrokeSet pens)
    {
        var rad45 = Math.PI / 4;
        var c = Math.Cos(rad45);
        var s = Math.Sin(rad45);
        TickArm(dc, ch, cx, cy, c, s, length, gap, scale, pens, ch.ShowTopLine || ch.ShowRightLine);
        TickArm(dc, ch, cx, cy, -c, -s, length, gap, scale, pens, ch.ShowBottomLine || ch.ShowLeftLine);
        TickArm(dc, ch, cx, cy, -c, s, length, gap, scale, pens, ch.ShowTopLine || ch.ShowLeftLine);
        TickArm(dc, ch, cx, cy, c, -s, length, gap, scale, pens, ch.ShowBottomLine || ch.ShowRightLine);
    }

    private static void TickArm(DrawingContext dc, Crosshair ch, double cx, double cy, double dirX, double dirY, double half, double gap, double scale, StrokeSet pens, bool show)
    {
        if (!show)
            return;
        var n = Math.Clamp((int)Math.Round(ch.TickCount), 1, 8);
        var tick = ch.TickLength * scale;
        var space = Math.Max(1.5 * scale, ch.TickSpacing * scale);
        var nx = -dirY;
        var ny = dirX;
        for (var i = 1; i <= n; i++)
        {
            var d = gap + i * space;
            if (d >= half - 0.5)
                break;
            var px = cx + dirX * d;
            var py = cy + dirY * d;
            Segment(dc, new Point(px - nx * tick, py - ny * tick), new Point(px + nx * tick, py + ny * tick), pens);
        }
    }

    private static void DrawAxisMidDots(DrawingContext dc, Crosshair ch, double cx, double cy, double halfW, double halfH, double gapX, double gapY, double scale)
    {
        MidDot(dc, ch, cx, cy, 1, 0, halfW, gapX, scale, ch.ShowRightLine);
        MidDot(dc, ch, cx, cy, -1, 0, halfW, gapX, scale, ch.ShowLeftLine);
        MidDot(dc, ch, cx, cy, 0, 1, halfH, gapY, scale, ch.ShowBottomLine);
        MidDot(dc, ch, cx, cy, 0, -1, halfH, gapY, scale, ch.ShowTopLine);
    }

    private static void DrawDiagonalMidDots(DrawingContext dc, Crosshair ch, double cx, double cy, double length, double gap, double scale)
    {
        var c = Math.Cos(Math.PI / 4);
        var s = Math.Sin(Math.PI / 4);
        MidDot(dc, ch, cx, cy, c, s, length, gap, scale, ch.ShowTopLine || ch.ShowRightLine);
        MidDot(dc, ch, cx, cy, -c, -s, length, gap, scale, ch.ShowBottomLine || ch.ShowLeftLine);
        MidDot(dc, ch, cx, cy, -c, s, length, gap, scale, ch.ShowTopLine || ch.ShowLeftLine);
        MidDot(dc, ch, cx, cy, c, -s, length, gap, scale, ch.ShowBottomLine || ch.ShowRightLine);
    }

    private static void MidDot(DrawingContext dc, Crosshair ch, double cx, double cy, double dirX, double dirY, double half, double gap, double scale, bool show)
    {
        if (!show)
            return;
        var d = (gap + half) * 0.5;
        DrawDot(dc, ch, cx + dirX * d, cy + dirY * d, Math.Max(1, ch.MidDotSize) * scale, ch.Color, ch.DotShape, scale);
    }

    private static void DrawPolygon(DrawingContext dc, Crosshair ch, double cx, double cy, double scale, int sides, double startDeg)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var rx = ch.AxisWidth * scale / 2;
        var ry = ch.AxisHeight * scale / 2;
        var points = new Point[sides];
        for (var i = 0; i < sides; i++)
        {
            var angle = (startDeg + i * 360.0 / sides) * Math.PI / 180;
            points[i] = new Point(cx + rx * Math.Cos(angle), cy + ry * Math.Sin(angle));
        }

        PaintGeometry(dc, Polygon(points, ch.FillShape), pens, ch.FillShape ? Fill(ch, ch.Color) : null);
    }

    private static void DrawBrackets(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var halfH = ch.AxisHeight * scale / 2;
        var length = Math.Max(4, ch.AxisWidth * scale / 4);
        var inner = ch.GapX * scale + 2 * scale;
        if (ch.ShowLeftLine)
        {
            Segment(dc, new Point(cx - inner - length, cy - halfH), new Point(cx - inner, cy - halfH), pens);
            Segment(dc, new Point(cx - inner - length, cy - halfH), new Point(cx - inner - length, cy + halfH), pens);
            Segment(dc, new Point(cx - inner - length, cy + halfH), new Point(cx - inner, cy + halfH), pens);
        }

        if (ch.ShowRightLine)
        {
            Segment(dc, new Point(cx + inner, cy - halfH), new Point(cx + inner + length, cy - halfH), pens);
            Segment(dc, new Point(cx + inner + length, cy - halfH), new Point(cx + inner + length, cy + halfH), pens);
            Segment(dc, new Point(cx + inner, cy + halfH), new Point(cx + inner + length, cy + halfH), pens);
        }
    }

    private static void DrawChevrons(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        var pens = Stroke(ch, ch.Color, ch.Thickness * scale, scale);
        var halfH = ch.AxisHeight * scale / 2;
        var depth = Math.Max(4, ch.AxisWidth * scale / 3);
        var gap = ch.GapX * scale;
        if (ch.ShowLeftLine)
        {
            Segment(dc, new Point(cx - gap - depth, cy - halfH), new Point(cx - gap, cy), pens);
            Segment(dc, new Point(cx - gap, cy), new Point(cx - gap - depth, cy + halfH), pens);
        }

        if (ch.ShowRightLine)
        {
            Segment(dc, new Point(cx + gap + depth, cy - halfH), new Point(cx + gap, cy), pens);
            Segment(dc, new Point(cx + gap, cy), new Point(cx + gap + depth, cy + halfH), pens);
        }

        if (ch.ShowTopLine)
        {
            Segment(dc, new Point(cx - halfH, cy - gap - depth), new Point(cx, cy - gap), pens);
            Segment(dc, new Point(cx, cy - gap), new Point(cx + halfH, cy - gap - depth), pens);
        }

        if (ch.ShowBottomLine)
        {
            Segment(dc, new Point(cx - halfH, cy + gap + depth), new Point(cx, cy + gap), pens);
            Segment(dc, new Point(cx, cy + gap), new Point(cx + halfH, cy + gap + depth), pens);
        }
    }

    private static void DrawExtraParts(DrawingContext dc, Crosshair ch, double cx, double cy, double scale)
    {
        if (ch.ExtraParts is not { Count: > 0 })
            return;

        foreach (var part in ch.ExtraParts)
        {
            if (!part.Enabled)
                continue;
            var px = cx + part.OffsetX * scale;
            var py = cy + part.OffsetY * scale;
            if (Math.Abs(part.Rotation) > 0.01)
            {
                var matrix = Matrix.CreateTranslation(-px, -py)
                             * Matrix.CreateRotation(part.Rotation * Math.PI / 180.0)
                             * Matrix.CreateTranslation(px, py);
                using (dc.PushTransform(matrix))
                    DrawPart(dc, ch, part, px, py, scale);
            }
            else
            {
                DrawPart(dc, ch, part, px, py, scale);
            }
        }
    }

    private static void DrawPart(DrawingContext dc, Crosshair ch, CrosshairPart part, double cx, double cy, double scale)
    {
        var opacity = Math.Clamp(part.Opacity, 0.05, 1);
        var pens = Stroke(ch, part.Color, Math.Max(0.5, part.Thickness) * scale, scale, opacity);
        var size = part.Size * scale;
        var gap = part.Gap * scale;
        switch (part.Kind)
        {
            case OverlayPartKind.Ring:
                PaintEllipse(dc, new Point(cx, cy), size / 2, size / 2, pens, null);
                break;
            case OverlayPartKind.Dot:
                DrawDot(dc, ch, cx, cy, Math.Max(2, size / 4), part.Color, DotShape.Circle, scale);
                break;
            case OverlayPartKind.Cross:
                DrawSimpleCross(dc, cx, cy, size / 2, gap, pens, false);
                break;
            case OverlayPartKind.CrossX:
                DrawSimpleCross(dc, cx, cy, size / 2, gap, pens, true);
                break;
            case OverlayPartKind.Arc:
            {
                var sweep = 270;
                Point? prev = null;
                for (var i = 0; i <= 36; i++)
                {
                    var t = -90 + sweep * i / 36.0;
                    var rad = t * Math.PI / 180;
                    var point = new Point(cx + size / 2 * Math.Cos(rad), cy + size / 2 * Math.Sin(rad));
                    if (prev != null)
                        Segment(dc, prev.Value, point, pens);
                    prev = point;
                }
                break;
            }
            case OverlayPartKind.Diamond:
            {
                var geo = Polygon(
                [
                    new Point(cx, cy - size / 2),
                    new Point(cx + size / 2, cy),
                    new Point(cx, cy + size / 2),
                    new Point(cx - size / 2, cy)
                ], false);
                PaintGeometry(dc, geo, pens, null);
                break;
            }
            case OverlayPartKind.Square:
                if (pens.Outline != null)
                    dc.DrawRectangle(null, pens.Outline, new Rect(cx - size / 2, cy - size / 2, size, size));
                dc.DrawRectangle(null, pens.Main, new Rect(cx - size / 2, cy - size / 2, size, size));
                break;
            case OverlayPartKind.Ticks:
                TickArm(dc, ch, cx, cy, 1, 0, size / 2, gap, scale, pens, true);
                TickArm(dc, ch, cx, cy, -1, 0, size / 2, gap, scale, pens, true);
                TickArm(dc, ch, cx, cy, 0, 1, size / 2, gap, scale, pens, true);
                TickArm(dc, ch, cx, cy, 0, -1, size / 2, gap, scale, pens, true);
                break;
            case OverlayPartKind.Brackets:
            {
                var halfH = size / 2;
                var length = Math.Max(4, size / 4);
                var inner = gap + 2 * scale;
                Segment(dc, new Point(cx - inner - length, cy - halfH), new Point(cx - inner, cy - halfH), pens);
                Segment(dc, new Point(cx - inner - length, cy - halfH), new Point(cx - inner - length, cy + halfH), pens);
                Segment(dc, new Point(cx - inner - length, cy + halfH), new Point(cx - inner, cy + halfH), pens);
                Segment(dc, new Point(cx + inner, cy - halfH), new Point(cx + inner + length, cy - halfH), pens);
                Segment(dc, new Point(cx + inner + length, cy - halfH), new Point(cx + inner + length, cy + halfH), pens);
                Segment(dc, new Point(cx + inner, cy + halfH), new Point(cx + inner + length, cy + halfH), pens);
                break;
            }
        }
    }

    private static void DrawSimpleCross(DrawingContext dc, double cx, double cy, double half, double gap, StrokeSet pens, bool diagonal)
    {
        if (diagonal)
        {
            DrawGappedDiagonal(dc, cx, cy, half, gap, 45, pens, true, true);
            DrawGappedDiagonal(dc, cx, cy, half, gap, 135, pens, true, true);
            return;
        }

        Segment(dc, new Point(cx + gap, cy), new Point(cx + half, cy), pens);
        Segment(dc, new Point(cx - gap, cy), new Point(cx - half, cy), pens);
        Segment(dc, new Point(cx, cy + gap), new Point(cx, cy + half), pens);
        Segment(dc, new Point(cx, cy - gap), new Point(cx, cy - half), pens);
    }

    private static void DrawRing(DrawingContext dc, Crosshair ch, double cx, double cy, double radius, double thickness, uint color, double scale)
        => PaintEllipse(dc, new Point(cx, cy), radius, radius, Stroke(ch, color, thickness, scale), null);

    private static void DrawDot(DrawingContext dc, Crosshair ch, double cx, double cy, double size, uint color, DotShape shape, double scale)
    {
        var fill = Fill(ch, color);
        var outlineW = ch.ShowOutline ? Math.Max(1.4, ch.OutlineThickness * scale) : 0;
        var glow = ch.ShowGlow ? GlowPen(ch, size * 0.3) : null;
        var center = new Point(cx, cy);
        var half = size / 2;
        switch (shape)
        {
            case DotShape.Square:
            {
                if (glow != null)
                    dc.DrawRectangle(null, glow, new Rect(cx - half, cy - half, size, size));
                if (outlineW > 0)
                    dc.DrawRectangle(new SolidColorBrush(ToColor(ch.OutlineColor)) { Opacity = ch.Opacity }, null,
                        new Rect(cx - half - outlineW, cy - half - outlineW, size + outlineW * 2, size + outlineW * 2));
                dc.DrawRectangle(fill, null, new Rect(cx - half, cy - half, size, size));
                return;
            }
            case DotShape.Diamond:
            {
                var geo = Polygon(
                [
                    new Point(cx, cy - half),
                    new Point(cx + half, cy),
                    new Point(cx, cy + half),
                    new Point(cx - half, cy)
                ], true);
                if (glow != null) dc.DrawGeometry(null, glow, geo);
                if (outlineW > 0)
                {
                    var outer = half + outlineW;
                    var ring = Polygon(
                    [
                        new Point(cx, cy - outer),
                        new Point(cx + outer, cy),
                        new Point(cx, cy + outer),
                        new Point(cx - outer, cy)
                    ], true);
                    dc.DrawGeometry(new SolidColorBrush(ToColor(ch.OutlineColor)) { Opacity = ch.Opacity }, null, ring);
                }
                dc.DrawGeometry(fill, null, geo);
                return;
            }
            case DotShape.Plus:
            {
                var pens = Stroke(ch, color, Math.Max(0.8, size * 0.28), scale);
                Segment(dc, new Point(cx - half, cy), new Point(cx + half, cy), pens);
                Segment(dc, new Point(cx, cy - half), new Point(cx, cy + half), pens);
                return;
            }
            case DotShape.Ring:
                PaintEllipse(dc, center, half, half, Stroke(ch, color, Math.Max(0.8, size * 0.22), scale), null);
                return;
            default:
                if (glow != null) dc.DrawEllipse(null, glow, center, half, half);
                if (outlineW > 0)
                    dc.DrawEllipse(new SolidColorBrush(ToColor(ch.OutlineColor)) { Opacity = ch.Opacity }, null, center, half + outlineW, half + outlineW);
                dc.DrawEllipse(fill, null, center, half, half);
                return;
        }
    }

    private static void PaintEllipse(DrawingContext dc, Point center, double rx, double ry, StrokeSet pens, IBrush? fill)
    {
        if (pens.Glow != null) dc.DrawEllipse(null, pens.Glow, center, rx, ry);
        if (pens.Outline != null) dc.DrawEllipse(null, pens.Outline, center, rx, ry);
        dc.DrawEllipse(fill, pens.Main, center, rx, ry);
    }

    private static void PaintGeometry(DrawingContext dc, Geometry geometry, StrokeSet pens, IBrush? fill)
    {
        if (pens.Glow != null) dc.DrawGeometry(null, pens.Glow, geometry);
        if (pens.Outline != null) dc.DrawGeometry(null, pens.Outline, geometry);
        dc.DrawGeometry(fill, pens.Main, geometry);
    }

    private static void Segment(DrawingContext dc, Point a, Point b, StrokeSet pens)
    {
        if (pens.Glow != null) dc.DrawLine(pens.Glow, a, b);
        if (pens.Outline != null) dc.DrawLine(pens.Outline, a, b);
        dc.DrawLine(pens.Main, a, b);
    }

    private static StrokeSet Stroke(Crosshair ch, uint color, double thickness, double scale, double opacityScale = 1)
    {
        var opacity = Math.Clamp(ch.Opacity * opacityScale, 0.05, 1);
        var main = PenFrom(color, opacity, Math.Max(0.5, thickness), ch.LineCap);
        Pen? outline = null;
        if (ch.ShowOutline && ch.OutlineThickness > 0)
        {
            var pad = Math.Max(1.5, ch.OutlineThickness * scale * 2);
            outline = PenFrom(ch.OutlineColor, Math.Min(1, opacity + 0.2), thickness + pad, LineCapKind.Round);
        }

        Pen? glow = ch.ShowGlow && ch.GlowAmount > 0 ? GlowPen(ch, thickness) : null;
        return new StrokeSet(main, outline, glow);
    }

    private static Pen GlowPen(Crosshair ch, double thickness)
        => PenFrom(ch.GlowColor, ch.Opacity * 0.28, thickness + ch.GlowAmount, LineCapKind.Round);

    private static Pen PenFrom(uint argb, double opacity, double thickness, LineCapKind cap)
    {
        var color = ToColor(argb);
        var brush = new SolidColorBrush(color) { Opacity = opacity };
        var lineCap = cap switch
        {
            LineCapKind.Round => PenLineCap.Round,
            LineCapKind.Square => PenLineCap.Square,
            _ => PenLineCap.Flat
        };
        return new Pen(brush, Math.Max(0.5, thickness))
        {
            LineCap = lineCap,
            LineJoin = lineCap == PenLineCap.Round ? PenLineJoin.Round : PenLineJoin.Miter
        };
    }

    private static SolidColorBrush Fill(Crosshair ch, uint argb)
        => new(ToColor(argb)) { Opacity = ch.Opacity };

    public static Color ToColor(uint argb)
        => Color.FromUInt32(argb);

    private static Geometry Polygon(IReadOnlyList<Point> points, bool filled)
    {
        var geo = new StreamGeometry();
        using var ctx = geo.Open();
        ctx.BeginFigure(points[0], filled);
        for (var i = 1; i < points.Count; i++)
            ctx.LineTo(points[i]);
        ctx.EndFigure(true);
        return geo;
    }

    private static Point Offset(Point origin, double dx, double dy, double length)
        => new(origin.X + dx * length, origin.Y + dy * length);

    private readonly record struct StrokeSet(Pen Main, Pen? Outline, Pen? Glow);
}
