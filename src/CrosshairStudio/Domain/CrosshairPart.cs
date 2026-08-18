using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CrosshairStudio.Infrastructure.Localization;

namespace CrosshairStudio.Domain;

public partial class CrosshairPart : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty] private OverlayPartKind _kind = OverlayPartKind.Ring;
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private uint _color = 0xFFFFFFFF;
    [ObservableProperty] private double _size = 28;
    [ObservableProperty] private double _thickness = 1.5;
    [ObservableProperty] private double _opacity = 1;
    [ObservableProperty] private double _gap = 4;
    [ObservableProperty] private double _rotation;
    [ObservableProperty] private double _offsetX;
    [ObservableProperty] private double _offsetY;

    [JsonIgnore]
    public string TitleKey => Kind switch
    {
        OverlayPartKind.Dot => "partDot",
        OverlayPartKind.Cross => "partCross",
        OverlayPartKind.CrossX => "partX",
        OverlayPartKind.Arc => "partArc",
        OverlayPartKind.Diamond => "partDiamond",
        OverlayPartKind.Square => "partSquare",
        OverlayPartKind.Ticks => "partTicks",
        OverlayPartKind.Brackets => "partBrackets",
        _ => "partRing"
    };

    [JsonIgnore]
    public string Title => Loc.Current[TitleKey];

    public CrosshairPart Clone() => new()
    {
        Kind = Kind,
        Enabled = Enabled,
        Color = Color,
        Size = Size,
        Thickness = Thickness,
        Opacity = Opacity,
        Gap = Gap,
        Rotation = Rotation,
        OffsetX = OffsetX,
        OffsetY = OffsetY
    };
}

public enum OverlayPartKind
{
    Ring,
    Dot,
    Cross,
    CrossX,
    Arc,
    Diamond,
    Square,
    Ticks,
    Brackets
}
