using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrosshairStudio.Domain;

public partial class Crosshair : ObservableObject
{
    public string Id { get; set; } = "";
    [ObservableProperty] private string _name = "New Crosshair";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private bool _isUser;
    [ObservableProperty] private bool _listedInWorkshop;
    public string? WorkshopId { get; set; }
    [ObservableProperty] private uint _color = 0xFFFFFFFF;
    [ObservableProperty] private double _size = 18;
    [ObservableProperty] private double _thickness = 2;
    [ObservableProperty] private double _opacity = 1;
    [ObservableProperty] private CrosshairType _type = CrosshairType.Cross;

    [ObservableProperty] private bool _showCenterDot;
    [ObservableProperty] private uint _dotColor = 0xFFFFFFFF;
    [ObservableProperty] private double _dotSize = 3;
    [ObservableProperty] private DotShape _dotShape = DotShape.Circle;

    [ObservableProperty] private bool _showCrosshairLines = true;
    [ObservableProperty] private uint _lineColor = 0xFFFFFFFF;
    [ObservableProperty] private double _lineLength = 8;
    [ObservableProperty] private double _lineThickness = 2;
    [ObservableProperty] private double _gap = 3;

    [ObservableProperty] private bool _showTopLine = true;
    [ObservableProperty] private bool _showBottomLine = true;
    [ObservableProperty] private bool _showLeftLine = true;
    [ObservableProperty] private bool _showRightLine = true;

    [ObservableProperty] private bool _showOutline = true;
    [ObservableProperty] private uint _outlineColor = 0xFF000000;
    [ObservableProperty] private double _outlineThickness = 2;

    [ObservableProperty] private double _offsetX;
    [ObservableProperty] private double _offsetY;
    [ObservableProperty] private double _rotation;
    [ObservableProperty] private bool _useIndependentAxes;
    [ObservableProperty] private double _horizontalLength = 18;
    [ObservableProperty] private double _verticalLength = 18;

    [ObservableProperty] private bool _showGlow;
    [ObservableProperty] private uint _glowColor = 0xFFFFFFFF;
    [ObservableProperty] private double _glowAmount = 6;

    [ObservableProperty] private LineCapKind _lineCap = LineCapKind.Flat;
    [ObservableProperty] private bool _fillShape;

    [ObservableProperty] private bool _showInnerCircle;
    [ObservableProperty] private uint _innerCircleColor = 0xFFFFFFFF;
    [ObservableProperty] private double _innerCircleSize = 10;
    [ObservableProperty] private double _innerCircleThickness = 1;

    [ObservableProperty] private bool _enablePulse;
    [ObservableProperty] private double _pulseSpeed = 1.2;
    [ObservableProperty] private double _pulseAmount = 0.1;

    [ObservableProperty] private bool _useAsymmetricGap;
    [ObservableProperty] private double _horizontalGap = 3;
    [ObservableProperty] private double _verticalGap = 3;
    [ObservableProperty] private double _cornerRadius;
    [ObservableProperty] private double _arcDegrees = 360;

    [ObservableProperty] private bool _showOuterRing;
    [ObservableProperty] private uint _outerRingColor = 0xFFFFFFFF;
    [ObservableProperty] private double _outerRingSize = 28;
    [ObservableProperty] private double _outerRingThickness = 1;

    [ObservableProperty] private bool _showTicks;
    [ObservableProperty] private double _tickCount = 2;
    [ObservableProperty] private double _tickLength = 3;
    [ObservableProperty] private double _tickSpacing = 4;

    [ObservableProperty] private bool _showMidDots;
    [ObservableProperty] private double _midDotSize = 2;
    [ObservableProperty] private bool _showSideBrackets;

    public ObservableCollection<CrosshairPart> ExtraParts
    {
        get => _extraParts ??= [];
        set
        {
            if (ReferenceEquals(_extraParts, value))
                return;
            _extraParts = value ?? [];
            OnPropertyChanged();
        }
    }

    private ObservableCollection<CrosshairPart>? _extraParts = [];

    [JsonIgnore] public double AxisWidth => UseIndependentAxes ? HorizontalLength : Size;
    [JsonIgnore] public double AxisHeight => UseIndependentAxes ? VerticalLength : Size;
    [JsonIgnore] public double GapX => UseAsymmetricGap ? HorizontalGap : Gap;
    [JsonIgnore] public double GapY => UseAsymmetricGap ? VerticalGap : Gap;

    public Crosshair Clone()
    {
        var copy = new Crosshair
        {
            Id = "",
            IsUser = false,
            ListedInWorkshop = false,
            WorkshopId = null,
            Name = Name + " Copy",
            Description = Description,
            Color = Color,
            Size = Size,
            Thickness = Thickness,
            Opacity = Opacity,
            Type = Type,
            ShowCenterDot = ShowCenterDot,
            DotColor = DotColor,
            DotSize = DotSize,
            DotShape = DotShape,
            ShowCrosshairLines = ShowCrosshairLines,
            LineColor = LineColor,
            LineLength = LineLength,
            LineThickness = LineThickness,
            Gap = Gap,
            ShowTopLine = ShowTopLine,
            ShowBottomLine = ShowBottomLine,
            ShowLeftLine = ShowLeftLine,
            ShowRightLine = ShowRightLine,
            ShowOutline = ShowOutline,
            OutlineColor = OutlineColor,
            OutlineThickness = OutlineThickness,
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            Rotation = Rotation,
            UseIndependentAxes = UseIndependentAxes,
            HorizontalLength = HorizontalLength,
            VerticalLength = VerticalLength,
            ShowGlow = ShowGlow,
            GlowColor = GlowColor,
            GlowAmount = GlowAmount,
            LineCap = LineCap,
            FillShape = FillShape,
            ShowInnerCircle = ShowInnerCircle,
            InnerCircleColor = InnerCircleColor,
            InnerCircleSize = InnerCircleSize,
            InnerCircleThickness = InnerCircleThickness,
            EnablePulse = EnablePulse,
            PulseSpeed = PulseSpeed,
            PulseAmount = PulseAmount,
            UseAsymmetricGap = UseAsymmetricGap,
            HorizontalGap = HorizontalGap,
            VerticalGap = VerticalGap,
            CornerRadius = CornerRadius,
            ArcDegrees = ArcDegrees,
            ShowOuterRing = ShowOuterRing,
            OuterRingColor = OuterRingColor,
            OuterRingSize = OuterRingSize,
            OuterRingThickness = OuterRingThickness,
            ShowTicks = ShowTicks,
            TickCount = TickCount,
            TickLength = TickLength,
            TickSpacing = TickSpacing,
            ShowMidDots = ShowMidDots,
            MidDotSize = MidDotSize,
            ShowSideBrackets = ShowSideBrackets
        };
        foreach (var part in ExtraParts)
            copy.ExtraParts.Add(part.Clone());
        return copy;
    }

    public void CopyFrom(Crosshair src)
    {
        Name = src.Name;
        Description = src.Description;
        Color = src.Color;
        Size = src.Size;
        Thickness = src.Thickness;
        Opacity = src.Opacity;
        Type = src.Type;
        ShowCenterDot = src.ShowCenterDot;
        DotColor = src.DotColor;
        DotSize = src.DotSize;
        DotShape = src.DotShape;
        ShowCrosshairLines = src.ShowCrosshairLines;
        LineColor = src.LineColor;
        LineLength = src.LineLength;
        LineThickness = src.LineThickness;
        Gap = src.Gap;
        ShowTopLine = src.ShowTopLine;
        ShowBottomLine = src.ShowBottomLine;
        ShowLeftLine = src.ShowLeftLine;
        ShowRightLine = src.ShowRightLine;
        ShowOutline = src.ShowOutline;
        OutlineColor = src.OutlineColor;
        OutlineThickness = src.OutlineThickness;
        OffsetX = src.OffsetX;
        OffsetY = src.OffsetY;
        Rotation = src.Rotation;
        UseIndependentAxes = src.UseIndependentAxes;
        HorizontalLength = src.HorizontalLength;
        VerticalLength = src.VerticalLength;
        ShowGlow = src.ShowGlow;
        GlowColor = src.GlowColor;
        GlowAmount = src.GlowAmount;
        LineCap = src.LineCap;
        FillShape = src.FillShape;
        ShowInnerCircle = src.ShowInnerCircle;
        InnerCircleColor = src.InnerCircleColor;
        InnerCircleSize = src.InnerCircleSize;
        InnerCircleThickness = src.InnerCircleThickness;
        EnablePulse = src.EnablePulse;
        PulseSpeed = src.PulseSpeed;
        PulseAmount = src.PulseAmount;
        UseAsymmetricGap = src.UseAsymmetricGap;
        HorizontalGap = src.HorizontalGap;
        VerticalGap = src.VerticalGap;
        CornerRadius = src.CornerRadius;
        ArcDegrees = src.ArcDegrees;
        ShowOuterRing = src.ShowOuterRing;
        OuterRingColor = src.OuterRingColor;
        OuterRingSize = src.OuterRingSize;
        OuterRingThickness = src.OuterRingThickness;
        ShowTicks = src.ShowTicks;
        TickCount = src.TickCount;
        TickLength = src.TickLength;
        TickSpacing = src.TickSpacing;
        ShowMidDots = src.ShowMidDots;
        MidDotSize = src.MidDotSize;
        ShowSideBrackets = src.ShowSideBrackets;
        ExtraParts.Clear();
        foreach (var part in src.ExtraParts)
            ExtraParts.Add(part.Clone());
    }
}

public enum CrosshairType
{
    Cross,
    CrossX,
    Circle,
    Dot,
    Square,
    Diamond,
    Triangle,
    Hexagon,
    Brackets,
    Chevron,
    Star,
    Arc,
    Custom
}

public enum DotShape
{
    Circle,
    Square,
    Diamond,
    Plus,
    Ring
}

public enum LineCapKind
{
    Flat,
    Round,
    Square
}
