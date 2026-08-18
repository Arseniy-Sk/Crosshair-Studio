using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CrosshairStudio.Infrastructure.Localization;

namespace CrosshairStudio.Domain;

public partial class HudWidget : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty] private WidgetKind _kind;
    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private WidgetAnchor _anchor = WidgetAnchor.TopRight;
    [ObservableProperty] private int _offsetX;
    [ObservableProperty] private int _offsetY = 16;
    [ObservableProperty] private uint _color = 0xFFFFFFFF;
    [ObservableProperty] private double _opacity = 1;
    [ObservableProperty] private string _text = "Shift sprint";
    [ObservableProperty] private string _host = "1.1.1.1";
    [ObservableProperty] private int _countdownSeconds = 40;
    [ObservableProperty] private bool _running;
    [ObservableProperty] private int _count;
    [ObservableProperty] private int _scoreLeft;
    [ObservableProperty] private int _scoreRight;
    [ObservableProperty] private bool _pinnedLayout;
    [ObservableProperty] private int _layoutX;
    [ObservableProperty] private int _layoutY;
    [ObservableProperty] private double _scale = 1;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _isUser;
    [ObservableProperty] private bool _listedInWorkshop;
    [ObservableProperty] private WidgetKind _function = WidgetKind.Notes;
    [ObservableProperty] private double _zoom = 3;
    [ObservableProperty] private int _captureSize = 88;
    [ObservableProperty] private bool _followCursor;
    [ObservableProperty] private int _captureOffsetX;
    [ObservableProperty] private int _captureOffsetY;
    [ObservableProperty] private WidgetChrome _chrome = WidgetChrome.Glass;
    [ObservableProperty] private string _imagePath = "";
    [ObservableProperty] private WidgetClickAction _clickAction = WidgetClickAction.None;
    [ObservableProperty] private ZoomTrigger _zoomTrigger = ZoomTrigger.Click;
    [ObservableProperty] private int _triggerVirtualKey;
    [ObservableProperty] private string _triggerKeyName = "";
    [ObservableProperty] private bool _triggerCtrl;
    [ObservableProperty] private bool _triggerShift;
    [ObservableProperty] private bool _triggerAlt;
    [ObservableProperty] private bool _useZoom;
    [ObservableProperty] private int _outputSize;
    [ObservableProperty] private int _lensX = 80;
    [ObservableProperty] private int _lensY = 80;
    [ObservableProperty] private bool _lensPinned;
    [ObservableProperty] private ZoomSource _zoomSource;
    [ObservableProperty] private int _sourceX = 200;
    [ObservableProperty] private int _sourceY = 200;
    [ObservableProperty] private int _noteWidth = 220;
    [ObservableProperty] private int _noteHeight = 168;
    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isBindingKey;
    public string? WorkshopId { get; set; }

    public WidgetKind EffectiveKind => Kind == WidgetKind.Custom ? Function : Kind;

    public string TitleKey => Kind switch
    {
        WidgetKind.Session => "widgetSession",
        WidgetKind.Stopwatch => "widgetStopwatch",
        WidgetKind.Countdown => "widgetCountdown",
        WidgetKind.System => "widgetSystem",
        WidgetKind.Ping => "widgetPing",
        WidgetKind.Notes => "widgetNotes",
        WidgetKind.Date => "widgetDate",
        WidgetKind.Battery => "widgetBattery",
        WidgetKind.Network => "widgetNetwork",
        WidgetKind.Uptime => "widgetUptime",
        WidgetKind.Display => "widgetDisplay",
        WidgetKind.Cpu => "widgetCpu",
        WidgetKind.Ram => "widgetRam",
        WidgetKind.Disk => "widgetDisk",
        WidgetKind.Counter => "widgetCounter",
        WidgetKind.Score => "widgetScore",
        WidgetKind.ActiveApp => "widgetActiveApp",
        WidgetKind.Custom => "widgetCustom",
        WidgetKind.Magnifier => "widgetMagnifier",
        WidgetKind.Template => "widgetTemplate",
        _ => "widgetClock"
    };

    [JsonIgnore]
    public string Title
    {
        get
        {
            if (Kind == WidgetKind.Custom)
                return string.IsNullOrWhiteSpace(Name) ? Loc.Current["widgetCustom"] : Name;
            if (Kind == WidgetKind.Notes)
            {
                var line = (Text ?? "").Split('\n')[0].Trim();
                return string.IsNullOrWhiteSpace(line) ? Loc.Current["widgetNote"] : (line.Length > 28 ? line[..28] + "…" : line);
            }

            return Loc.Current[TitleKey];
        }
    }
    [JsonIgnore]
    public string Hint => Kind == WidgetKind.Custom
        ? Loc.Current["widgetCustomHint"]
        : Loc.Current[TitleKey + "Hint"];
    [JsonIgnore] public bool IsNotes => EffectiveKind == WidgetKind.Notes;
    [JsonIgnore] public bool IsPing => EffectiveKind == WidgetKind.Ping;
    [JsonIgnore] public bool IsCountdown => EffectiveKind == WidgetKind.Countdown;
    [JsonIgnore] public bool IsStopwatch => EffectiveKind == WidgetKind.Stopwatch;
    [JsonIgnore] public bool IsCounter => EffectiveKind == WidgetKind.Counter;
    [JsonIgnore] public bool IsScore => EffectiveKind == WidgetKind.Score;
    [JsonIgnore] public bool IsCustom => Kind == WidgetKind.Custom;
    [JsonIgnore] public bool IsMagnifier => EffectiveKind == WidgetKind.Magnifier || UseZoom;
    [JsonIgnore] public bool IsTemplate => EffectiveKind == WidgetKind.Template;
    [JsonIgnore] public bool IsSticky => IsNotes;
    [JsonIgnore] public bool CanRemove => IsCustom || (IsNotes && IsUser);
    [JsonIgnore] public bool IsEditableText => EffectiveKind is WidgetKind.Notes or WidgetKind.Template;
    [JsonIgnore] public bool NeedsClick => (IsMagnifier && ZoomTrigger != ZoomTrigger.KeyHold) || (IsTemplate && ClickAction != WidgetClickAction.None);
    [JsonIgnore]
    public ZoomSource EffectiveZoomSource
        => ZoomSource != ZoomSource.Aim ? ZoomSource : (FollowCursor ? ZoomSource.Cursor : ZoomSource.Aim);
    [JsonIgnore] public bool HasTrigger => TriggerVirtualKey != 0;
    [JsonIgnore]
    public string TriggerTitle
    {
        get
        {
            if (IsBindingKey)
                return Loc.Current["widgetKeyPress"];
            if (TriggerVirtualKey == 0)
                return Loc.Current["widgetBindKey"];
            var parts = new List<string>();
            if (TriggerCtrl) parts.Add("Ctrl");
            if (TriggerShift) parts.Add("Shift");
            if (TriggerAlt) parts.Add("Alt");
            parts.Add(string.IsNullOrWhiteSpace(TriggerKeyName) ? "?" : TriggerKeyName);
            return string.Join(" + ", parts);
        }
    }

    [JsonIgnore]
    public FunctionOption FunctionChoice
    {
        get => FunctionOption.All.FirstOrDefault(o => o.Kind == Function) ?? FunctionOption.All[0];
        set
        {
            if (value != null)
                Function = value.Kind;
        }
    }

    [JsonIgnore]
    public ColorSlot? ColorSlot { get; private set; }

    public void AttachColors(IReadOnlyList<ColorSwatch> presets)
        => ColorSlot = new ColorSlot("sectionColor", presets, () => Color, c => Color = c);

    public void RefreshLoc()
    {
        OnPropertyChanged(nameof(IsNotes));
        OnPropertyChanged(nameof(IsPing));
        OnPropertyChanged(nameof(IsCountdown));
        OnPropertyChanged(nameof(IsStopwatch));
        OnPropertyChanged(nameof(IsCounter));
        OnPropertyChanged(nameof(IsScore));
        OnPropertyChanged(nameof(IsCustom));
        OnPropertyChanged(nameof(IsMagnifier));
        OnPropertyChanged(nameof(IsTemplate));
        OnPropertyChanged(nameof(IsSticky));
        OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(IsEditableText));
        OnPropertyChanged(nameof(NeedsClick));
        OnPropertyChanged(nameof(HasTrigger));
        OnPropertyChanged(nameof(TriggerTitle));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(EffectiveKind));
        OnPropertyChanged(nameof(EffectiveZoomSource));
        OnPropertyChanged(nameof(FunctionChoice));
        OnPropertyChanged(nameof(ChromeChoice));
        OnPropertyChanged(nameof(ClickChoice));
        OnPropertyChanged(nameof(ZoomTriggerChoice));
        OnPropertyChanged(nameof(ZoomSourceChoice));
    }

    [JsonIgnore]
    public ChromeOption ChromeChoice
    {
        get => ChromeOption.All.FirstOrDefault(o => o.Chrome == Chrome) ?? ChromeOption.All[0];
        set
        {
            if (value != null)
                Chrome = value.Chrome;
        }
    }

    [JsonIgnore]
    public ClickOption ClickChoice
    {
        get => ClickOption.All.FirstOrDefault(o => o.Action == ClickAction) ?? ClickOption.All[0];
        set
        {
            if (value != null)
                ClickAction = value.Action;
        }
    }

    [JsonIgnore]
    public ZoomTriggerOption ZoomTriggerChoice
    {
        get => ZoomTriggerOption.All.FirstOrDefault(o => o.Mode == ZoomTrigger) ?? ZoomTriggerOption.All[0];
        set
        {
            if (value != null)
                ZoomTrigger = value.Mode;
        }
    }

    [JsonIgnore]
    public ZoomSourceOption ZoomSourceChoice
    {
        get => ZoomSourceOption.All.FirstOrDefault(o => o.Source == EffectiveZoomSource) ?? ZoomSourceOption.All[0];
        set
        {
            if (value != null)
                ZoomSource = value.Source;
        }
    }

    public bool MatchesTrigger(int virtualKey, bool ctrl, bool shift, bool alt)
        => TriggerVirtualKey != 0
           && TriggerVirtualKey == virtualKey
           && (!TriggerCtrl || ctrl)
           && (!TriggerShift || shift)
           && (!TriggerAlt || alt);

    partial void OnFunctionChanged(WidgetKind value) => RefreshLoc();
    partial void OnKindChanged(WidgetKind value) => RefreshLoc();
    partial void OnChromeChanged(WidgetChrome value) => RefreshLoc();
    partial void OnClickActionChanged(WidgetClickAction value) => RefreshLoc();
    partial void OnZoomTriggerChanged(ZoomTrigger value) => RefreshLoc();
    partial void OnTriggerVirtualKeyChanged(int value) => RefreshLoc();
    partial void OnTriggerKeyNameChanged(string value) => RefreshLoc();
    partial void OnTriggerCtrlChanged(bool value) => RefreshLoc();
    partial void OnTriggerShiftChanged(bool value) => RefreshLoc();
    partial void OnTriggerAltChanged(bool value) => RefreshLoc();
    partial void OnUseZoomChanged(bool value) => RefreshLoc();
    partial void OnZoomSourceChanged(ZoomSource value)
    {
        FollowCursor = value == ZoomSource.Cursor;
        RefreshLoc();
    }
    partial void OnFollowCursorChanged(bool value)
    {
        if (value && ZoomSource != ZoomSource.Cursor)
            ZoomSource = ZoomSource.Cursor;
        else if (!value && ZoomSource == ZoomSource.Cursor)
            ZoomSource = ZoomSource.Aim;
    }
    partial void OnIsBindingKeyChanged(bool value) => RefreshLoc();
}

public enum WidgetKind
{
    Clock,
    Session,
    Stopwatch,
    Countdown,
    System,
    Ping,
    Notes,
    Date,
    Battery,
    Network,
    Uptime,
    Display,
    Cpu,
    Ram,
    Disk,
    Counter,
    Score,
    ActiveApp,
        Custom,
        Magnifier,
        Template
}

public sealed record FunctionOption(WidgetKind Kind, string TitleKey)
{
    public string Title => Loc.Current[TitleKey];

    public static IReadOnlyList<FunctionOption> All { get; } =
    [
        new(WidgetKind.Template, "widgetTemplate"),
        new(WidgetKind.Notes, "widgetNotes"),
        new(WidgetKind.Clock, "widgetClock"),
        new(WidgetKind.Date, "widgetDate"),
        new(WidgetKind.Session, "widgetSession"),
        new(WidgetKind.Stopwatch, "widgetStopwatch"),
        new(WidgetKind.Countdown, "widgetCountdown"),
        new(WidgetKind.Counter, "widgetCounter"),
        new(WidgetKind.Score, "widgetScore"),
        new(WidgetKind.Ping, "widgetPing"),
        new(WidgetKind.System, "widgetSystem"),
        new(WidgetKind.Cpu, "widgetCpu"),
        new(WidgetKind.Ram, "widgetRam"),
        new(WidgetKind.Magnifier, "widgetMagnifier")
    ];
}

public sealed record ChromeOption(WidgetChrome Chrome, string TitleKey)
{
    public string Title => Loc.Current[TitleKey];

    public static IReadOnlyList<ChromeOption> All { get; } =
    [
        new(WidgetChrome.Glass, "widgetChromeGlass"),
        new(WidgetChrome.Plain, "widgetChromePlain"),
        new(WidgetChrome.Frame, "widgetChromeFrame"),
        new(WidgetChrome.Sticky, "widgetChromeSticky")
    ];
}

public sealed record ClickOption(WidgetClickAction Action, string TitleKey)
{
    public string Title => Loc.Current[TitleKey];

    public static IReadOnlyList<ClickOption> All { get; } =
    [
        new(WidgetClickAction.None, "widgetClickNone"),
        new(WidgetClickAction.Increment, "widgetClickInc"),
        new(WidgetClickAction.Decrement, "widgetClickDec"),
        new(WidgetClickAction.Reset, "widgetClickReset"),
        new(WidgetClickAction.Toggle, "widgetClickToggle")
    ];
}

public sealed record ZoomTriggerOption(ZoomTrigger Mode, string TitleKey)
{
    public string Title => Loc.Current[TitleKey];

    public static IReadOnlyList<ZoomTriggerOption> All { get; } =
    [
        new(ZoomTrigger.Click, "widgetZoomClick"),
        new(ZoomTrigger.KeyToggle, "widgetZoomToggle"),
        new(ZoomTrigger.KeyHold, "widgetZoomHold")
    ];
}

public sealed record ZoomSourceOption(ZoomSource Source, string TitleKey)
{
    public string Title => Loc.Current[TitleKey];

    public static IReadOnlyList<ZoomSourceOption> All { get; } =
    [
        new(ZoomSource.Aim, "widgetZoomAim"),
        new(ZoomSource.Cursor, "widgetFollowCursor"),
        new(ZoomSource.Pinned, "widgetZoomPinned")
    ];
}

public enum WidgetChrome
{
    Glass,
    Plain,
    Frame,
    Sticky
}

public enum WidgetClickAction
{
    None,
    Increment,
    Decrement,
    Reset,
    Toggle
}

public enum ZoomTrigger
{
    Click,
    KeyToggle,
    KeyHold
}

public enum ZoomSource
{
    Aim,
    Cursor,
    Pinned
}

public enum WidgetAnchor
{
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Top,
    Bottom
}
