using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosshairStudio.Domain;
using CrosshairStudio.Infrastructure;
using CrosshairStudio.Infrastructure.Localization;
using CrosshairStudio.Overlay;

namespace CrosshairStudio.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly JsonStore _store;
    private readonly OverlayService _overlay;
    private readonly AppSettings _appSettings;
    private readonly WorkshopClient _workshop;
    private readonly HotkeyHook _hotkey = new();
    private readonly InputWatch _input = new();
    private OverlayEditorWindow? _editor;
    private Crosshair? _hooked;
    private DispatcherTimer? _persist;
    private bool _layoutSync;
    private bool _closingEditor;
    private HudWidget? _bindingWidget;

    [ObservableProperty] private Crosshair _currentCrosshair;
    [ObservableProperty] private MonitorInfo? _selectedMonitor;
    [ObservableProperty] private string _section = "Shape";
    [ObservableProperty] private string _workspace = "Crosshair";
    [ObservableProperty] private bool _isOverlayVisible;
    [ObservableProperty] private uint _draftColor = 0xFFFFFFFF;
    [ObservableProperty] private bool _showOnboarding = true;
    [ObservableProperty] private CrosshairPart? _selectedPart;
    [ObservableProperty] private OverlayPartKind _draftPartKind = OverlayPartKind.Ring;
    [ObservableProperty] private bool _isCreateOpen;
    [ObservableProperty] private string _draftName = "";
    [ObservableProperty] private string _draftDescription = "";
    [ObservableProperty] private bool _draftListed;
    [ObservableProperty] private bool _isCreateWidgetOpen;
    [ObservableProperty] private string _draftWidgetName = "";
    [ObservableProperty] private string _draftWidgetText = "";
    [ObservableProperty] private bool _draftWidgetListed;
    [ObservableProperty] private WidgetKind _draftWidgetFunction = WidgetKind.Template;
    [ObservableProperty] private WidgetChrome _draftWidgetChrome = WidgetChrome.Glass;
    [ObservableProperty] private WidgetClickAction _draftWidgetClick;
    [ObservableProperty] private bool _draftUseZoom;
    [ObservableProperty] private bool _isCapturingHotkey;
    [ObservableProperty] private bool _isOverlayEditorOpen;

    public MainViewModel(JsonStore store, OverlayService overlay, AppSettings appSettings, WorkshopClient workshop)
    {
        _store = store;
        _overlay = overlay;
        _appSettings = appSettings;
        _workshop = workshop;
        Display = appSettings.Display ?? new DisplaySettings();
        appSettings.Display = Display;
        OverlayHotkey = appSettings.OverlayHotkey ?? HotkeyBinding.Default();
        if (OverlayHotkey.VirtualKey == 0)
            OverlayHotkey = HotkeyBinding.Default();
        appSettings.OverlayHotkey = OverlayHotkey;
        OverlayHotkey.PropertyChanged += (_, _) =>
        {
            OverlayHotkey.RefreshDisplay();
            OnPropertyChanged(nameof(HotkeyButtonTitle));
            OnPropertyChanged(nameof(OverlayEditorHint));
        };
        Loc.Current.SetLanguage(string.IsNullOrWhiteSpace(appSettings.Language) ? Loc.GuessLanguage() : appSettings.Language);
        if (string.IsNullOrWhiteSpace(appSettings.ClientId))
            appSettings.ClientId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(appSettings.WorkshopUrl))
            appSettings.WorkshopUrl = "http://150.251.152.203:8787";
        if (string.IsNullOrWhiteSpace(appSettings.DisplayName))
            appSettings.DisplayName = "Player";
        Onboarding = new OnboardingViewModel(() => ShowOnboarding = false);
        Loc.Current.LanguageChanged += OnLanguageChanged;
        Workshop = new WorkshopViewModel(workshop, this);

        var library = appSettings.Library is { Count: > 0 }
            ? appSettings.Library
            : store.LoadProfiles().Select(p => { p.IsUser = true; if (string.IsNullOrWhiteSpace(p.Id)) p.Id = Guid.NewGuid().ToString("N")[..8]; return p; }).ToList();
        foreach (var item in library)
        {
            item.ExtraParts ??= [];
            item.IsUser = true;
            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString("N")[..8];
            Customs.Add(item);
        }

        var saved = appSettings.Crosshair ?? Customs.FirstOrDefault() ?? CreateDefault("Crosshair");
        saved.ExtraParts ??= [];
        if (saved.IsUser && !string.IsNullOrWhiteSpace(saved.Id))
        {
            var match = Customs.FirstOrDefault(c => c.Id == saved.Id);
            CurrentCrosshair = match ?? saved;
            if (match == null)
                Customs.Add(saved);
        }
        else
        {
            CurrentCrosshair = saved;
        }
        DraftColor = CurrentCrosshair.Color;
        RebuildDesignTiles();
        RebuildColorSlots();

        var widgets = appSettings.Widgets is { Count: > 0 } ? appSettings.Widgets : CreateDefaultWidgets();
        foreach (var widget in widgets)
        {
            if (widget.Scale <= 0)
                widget.Scale = 1;
            widget.AttachColors(ColorPresets);
            Widgets.Add(widget);
            widget.PropertyChanged += OnWidgetChanged;
        }

        foreach (var extra in CreateDefaultWidgets())
        {
            if (Widgets.Any(w => w.Kind == extra.Kind))
                continue;
            extra.AttachColors(ColorPresets);
            Widgets.Add(extra);
            extra.PropertyChanged += OnWidgetChanged;
        }

        if (!Widgets.Any(w => w.Enabled))
        {
            foreach (var widget in Widgets.Where(w => w.Kind is WidgetKind.Clock or WidgetKind.Session or WidgetKind.System))
                widget.Enabled = true;
        }

        Display.PropertyChanged += (_, _) =>
        {
            PersistSettings();
            if (IsOverlayVisible)
                PushOverlay();
            OnPropertyChanged(nameof(StatusText));
        };

        _overlay.BindWidgetMoved(_ => SchedulePersist());
    }

    public Loc Loc => Loc.Current;
    public HotkeyBinding OverlayHotkey { get; }
    public WorkshopViewModel Workshop { get; }
    public DisplaySettings Display { get; }
    public OnboardingViewModel Onboarding { get; }

    public string DisplayName
    {
        get => _appSettings.DisplayName;
        set
        {
            if (_appSettings.DisplayName == value)
                return;
            _appSettings.DisplayName = value;
            PersistSettings();
            OnPropertyChanged();
        }
    }

    public string WorkshopUrl
    {
        get => _workshop.BaseUrl;
        set
        {
            _workshop.BaseUrl = value;
            PersistSettings();
            OnPropertyChanged();
        }
    }

    public ObservableCollection<MonitorInfo> Monitors { get; } = [];
    public ObservableCollection<HudWidget> Widgets { get; } = [];
    public ObservableCollection<Crosshair> Customs { get; } = [];
    public ObservableCollection<DesignTile> DesignTiles { get; } = [];
    public ObservableCollection<ColorSlot> ColorSlots { get; } = [];

    public IReadOnlyList<ShapeOption> Shapes { get; } =
    [
        Shape("shapeCross", CrosshairType.Cross),
        Shape("shapeX", CrosshairType.CrossX),
        Shape("shapeCircle", CrosshairType.Circle),
        Shape("shapeDot", CrosshairType.Dot),
        Shape("shapeSquare", CrosshairType.Square),
        Shape("shapeDiamond", CrosshairType.Diamond),
        Shape("shapeTriangle", CrosshairType.Triangle),
        Shape("shapeHex", CrosshairType.Hexagon),
        Shape("shapeBrackets", CrosshairType.Brackets),
        Shape("shapeChevron", CrosshairType.Chevron),
        Shape("shapeStar", CrosshairType.Star),
        Shape("shapeArc", CrosshairType.Arc)
    ];

    public IReadOnlyList<PartKindOption> PartKinds { get; } =
    [
        new(OverlayPartKind.Ring, "partRing"),
        new(OverlayPartKind.Dot, "partDot"),
        new(OverlayPartKind.Cross, "partCross"),
        new(OverlayPartKind.CrossX, "partX"),
        new(OverlayPartKind.Arc, "partArc"),
        new(OverlayPartKind.Diamond, "partDiamond"),
        new(OverlayPartKind.Square, "partSquare"),
        new(OverlayPartKind.Ticks, "partTicks"),
        new(OverlayPartKind.Brackets, "partBrackets")
    ];

    public IReadOnlyList<WidgetAnchor> Anchors { get; } = Enum.GetValues<WidgetAnchor>();
    public IReadOnlyList<FunctionOption> WidgetFunctions => FunctionOption.All;
    public IReadOnlyList<ChromeOption> ChromeOptions => ChromeOption.All;
    public IReadOnlyList<ClickOption> ClickOptions => ClickOption.All;
    public IReadOnlyList<ZoomTriggerOption> ZoomTriggerOptions => ZoomTriggerOption.All;
    public IReadOnlyList<ZoomSourceOption> ZoomSourceOptions => ZoomSourceOption.All;
    public IReadOnlyList<string> TemplateTokens { get; } =
        ["{time}", "{date}", "{hh}", "{mm}", "{ss}", "{count}", "{cpu}", "{ram}", "{ping}", "{session}", "{name}", "{left}", "{right}"];
    public IReadOnlyList<LineCapKind> LineCaps { get; } = [LineCapKind.Flat, LineCapKind.Round, LineCapKind.Square];
    public IReadOnlyList<DotShape> DotShapes { get; } = [DotShape.Circle, DotShape.Square, DotShape.Diamond, DotShape.Plus, DotShape.Ring];
    public IReadOnlyList<ColorSwatch> ColorPresets { get; } =
    [
        new() { Value = 0xFFFFFFFF },
        new() { Value = 0xFFD1D1D6 },
        new() { Value = 0xFFFFD60A },
        new() { Value = 0xFF32D74B },
        new() { Value = 0xFF64D2FF },
        new() { Value = 0xFFFF453A },
        new() { Value = 0xFFFF9F0A },
        new() { Value = 0xFF000000 }
    ];

    public IReadOnlyList<ColorSwatch> OutlinePresets { get; } =
    [
        new() { Value = 0xFF000000 },
        new() { Value = 0xFFFFFFFF },
        new() { Value = 0xFF1C1C1E },
        new() { Value = 0xFF8E8E93 }
    ];

    public LanguageOption? SelectedLanguage
    {
        get => Loc.Languages.FirstOrDefault(l => l.Code == Loc.Language);
        set
        {
            if (value == null || value.Code == Loc.Language)
                return;
            Loc.SetLanguage(value.Code);
            _appSettings.Language = value.Code;
            PersistSettings();
            OnPropertyChanged();
        }
    }

    public FunctionOption DraftFunctionChoice
    {
        get => FunctionOption.All.FirstOrDefault(o => o.Kind == DraftWidgetFunction) ?? FunctionOption.All[0];
        set
        {
            if (value == null)
                return;
            DraftWidgetFunction = value.Kind;
            OnPropertyChanged();
        }
    }

    public ChromeOption DraftChromeChoice
    {
        get => ChromeOption.All.FirstOrDefault(o => o.Chrome == DraftWidgetChrome) ?? ChromeOption.All[0];
        set
        {
            if (value == null)
                return;
            DraftWidgetChrome = value.Chrome;
            OnPropertyChanged();
        }
    }

    public ClickOption DraftClickChoice
    {
        get => ClickOption.All.FirstOrDefault(o => o.Action == DraftWidgetClick) ?? ClickOption.All[0];
        set
        {
            if (value == null)
                return;
            DraftWidgetClick = value.Action;
            OnPropertyChanged();
        }
    }

    public PartKindOption SelectedPartKind
    {
        get => PartKinds.First(p => p.Kind == DraftPartKind);
        set
        {
            if (value == null)
                return;
            DraftPartKind = value.Kind;
            OnPropertyChanged();
        }
    }

    public ShapeOption? SelectedShape
    {
        get => Shapes.FirstOrDefault(s => s.Type == CurrentCrosshair.Type);
        set
        {
            if (value == null || value.Type == CurrentCrosshair.Type)
                return;
            CurrentCrosshair.Type = value.Type;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedPart => SelectedPart != null;
    public bool IsUserDesign => CurrentCrosshair.IsUser;
    public ColorSlot? ShapeColorSlot => ColorSlots.Count > 0 ? ColorSlots[0] : null;
    public ColorSlot? SelectedPartColorSlot { get; private set; }

    public bool IsCrosshairWorkspace => Workspace == "Crosshair";
    public bool IsWidgetsWorkspace => Workspace == "Widgets";
    public bool IsWorkshopWorkspace => Workspace == "Workshop";
    public bool IsSettingsWorkspace => Workspace == "Settings";
    public bool IsShapeSection => Section == "Shape";
    public bool IsLookSection => Section == "Look";
    public bool IsLayersSection => Section == "Layers";
    public bool IsScreenSection => Section == "Screen";

    public bool ShowArmToggles => CurrentCrosshair.Type is CrosshairType.Cross or CrosshairType.CrossX
        or CrosshairType.Brackets or CrosshairType.Chevron or CrosshairType.Star or CrosshairType.Custom;
    public bool ShowCustomMetrics => CurrentCrosshair.Type == CrosshairType.Custom;
    public bool ShowFill => CurrentCrosshair.Type is CrosshairType.Circle or CrosshairType.Square
        or CrosshairType.Diamond or CrosshairType.Triangle or CrosshairType.Hexagon;
    public bool ShowGap => CurrentCrosshair.Type is CrosshairType.Cross or CrosshairType.CrossX
        or CrosshairType.Brackets or CrosshairType.Chevron or CrosshairType.Star or CrosshairType.Custom;
    public bool ShowTicksOption => CurrentCrosshair.Type is CrosshairType.Cross or CrosshairType.CrossX
        or CrosshairType.Custom;
    public bool ShowArcOption => CurrentCrosshair.Type is CrosshairType.Circle or CrosshairType.Arc;
    public bool ShowCornerRadius => CurrentCrosshair.Type == CrosshairType.Square;
    public bool ShowDotOptions => CurrentCrosshair.ShowCenterDot || CurrentCrosshair.Type == CrosshairType.Dot;

    public string StatusText
    {
        get
        {
            var live = IsOverlayVisible ? Loc["statusLive"] : Loc["statusReady"];
            var monitor = SelectedMonitor;
            return monitor == null ? live : $"{live}  ·  {monitor.Width}×{monitor.Height}  ·  {monitor.ScalePercent}%";
        }
    }

    public string OverlayButtonTitle => IsOverlayVisible ? Loc["overlayHide"] : Loc["overlayShow"];
    public string HotkeyButtonTitle => IsCapturingHotkey ? Loc["hotkeyPress"] : OverlayHotkey.Display;
    public string OverlayEditorHint => string.Format(Loc["overlayEditorHint"], OverlayHotkey.Display);
    public string PartsBoundHint => CurrentCrosshair.IsUser && !string.IsNullOrWhiteSpace(CurrentCrosshair.Name)
        ? string.Format(Loc["partsBoundTo"], CurrentCrosshair.Name)
        : Loc["partsBoundHint"];
    public int LayoutMaxX => Math.Max(320, SelectedMonitor?.Width ?? 1920);
    public int LayoutMaxY => Math.Max(240, SelectedMonitor?.Height ?? 1080);

    private Visual? _host;

    public void AttachHost(Visual host)
    {
        _host = host;
        RefreshMonitors(host);
        BindHotkey();
        _input.Changed -= OnGlobalInput;
        _input.Changed += OnGlobalInput;
        _input.Start();
    }

    [RelayCommand]
    private void RefreshDisplays()
    {
        if (_host != null)
            RefreshMonitors(_host);
    }

    public void RefreshMonitors(Visual host)
    {
        Monitors.Clear();
        foreach (var monitor in MonitorQuery.Get(host))
            Monitors.Add(monitor);

        if (Display.SelectedMonitorIndex >= Monitors.Count)
            Display.SelectedMonitorIndex = Monitors.FirstOrDefault(m => m.IsPrimary)?.Index ?? 0;

        SelectedMonitor = Monitors.Count == 0 ? null : Monitors[Math.Clamp(Display.SelectedMonitorIndex, 0, Monitors.Count - 1)];
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(LayoutMaxX));
        OnPropertyChanged(nameof(LayoutMaxY));
        if (IsOverlayVisible)
            PushOverlay();
    }

    partial void OnCurrentCrosshairChanged(Crosshair value)
    {
        Unhook(_hooked);
        Hook(value);
        _hooked = value;
        DraftColor = value.Color;
        SelectedPart = value.ExtraParts.FirstOrDefault();
        OnPropertyChanged(nameof(HasSelectedPart));
        OnPropertyChanged(nameof(IsUserDesign));
        OnPropertyChanged(nameof(PartsBoundHint));
        NotifyFlags();
        OnPropertyChanged(nameof(SelectedShape));
        RebuildColorSlots();
        RefreshDesignSelection();
        OnCrosshairChanged();
    }

    partial void OnSelectedMonitorChanged(MonitorInfo? value)
    {
        if (value != null)
            Display.SelectedMonitorIndex = value.Index;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(LayoutMaxX));
        OnPropertyChanged(nameof(LayoutMaxY));
        if (IsOverlayVisible)
            PushOverlay();
    }

    partial void OnDraftColorChanged(uint value) => CurrentCrosshair.Color = value;

    partial void OnSelectedPartChanged(CrosshairPart? value)
    {
        OnPropertyChanged(nameof(HasSelectedPart));
        RefreshPartColorSlot();
    }

    partial void OnSectionChanged(string value)
    {
        OnPropertyChanged(nameof(IsShapeSection));
        OnPropertyChanged(nameof(IsLookSection));
        OnPropertyChanged(nameof(IsLayersSection));
        OnPropertyChanged(nameof(IsScreenSection));
    }

    partial void OnWorkspaceChanged(string value)
    {
        OnPropertyChanged(nameof(IsCrosshairWorkspace));
        OnPropertyChanged(nameof(IsWidgetsWorkspace));
        OnPropertyChanged(nameof(IsWorkshopWorkspace));
        OnPropertyChanged(nameof(IsSettingsWorkspace));
        if (value == "Workshop")
            _ = Workshop.LoadAsync();
    }

    partial void OnDraftWidgetFunctionChanged(WidgetKind value) => OnPropertyChanged(nameof(DraftFunctionChoice));
    partial void OnDraftWidgetChromeChanged(WidgetChrome value) => OnPropertyChanged(nameof(DraftChromeChoice));
    partial void OnDraftWidgetClickChanged(WidgetClickAction value) => OnPropertyChanged(nameof(DraftClickChoice));

    [RelayCommand]
    private void SelectWorkspace(string workspace) => Workspace = workspace;

    [RelayCommand]
    private void SelectSection(string section) => Section = section;

    [RelayCommand]
    private void ApplyPreset(object? value)
    {
        if (!TryColor(value, out var color))
            return;
        DraftColor = color;
        CurrentCrosshair.Color = color;
    }

    [RelayCommand]
    private void ApplyPartColor(object? value)
    {
        if (SelectedPart == null || !TryColor(value, out var color))
            return;
        SelectedPart.Color = color;
    }

    [RelayCommand]
    private void ApplyDotColor(object? value)
    {
        if (!TryColor(value, out var color))
            return;
        CurrentCrosshair.DotColor = color;
        CurrentCrosshair.ShowCenterDot = true;
    }

    [RelayCommand]
    private void ApplyInnerColor(object? value)
    {
        if (!TryColor(value, out var color))
            return;
        CurrentCrosshair.InnerCircleColor = color;
        CurrentCrosshair.ShowInnerCircle = true;
    }

    [RelayCommand]
    private void ApplyOuterColor(object? value)
    {
        if (!TryColor(value, out var color))
            return;
        CurrentCrosshair.OuterRingColor = color;
        CurrentCrosshair.ShowOuterRing = true;
    }

    [RelayCommand]
    private void ApplyGlowColor(object? value)
    {
        if (!TryColor(value, out var color))
            return;
        CurrentCrosshair.GlowColor = color;
        CurrentCrosshair.ShowGlow = true;
    }

    [RelayCommand]
    private void ResetCrosshair()
    {
        var id = CurrentCrosshair.Id;
        var name = CurrentCrosshair.Name;
        var description = CurrentCrosshair.Description;
        var isUser = CurrentCrosshair.IsUser;
        var workshopId = CurrentCrosshair.WorkshopId;
        var listed = CurrentCrosshair.ListedInWorkshop;
        CurrentCrosshair.CopyFrom(CreateDefault(isUser ? name : "Crosshair"));
        CurrentCrosshair.Id = id;
        CurrentCrosshair.Name = name;
        CurrentCrosshair.Description = description;
        CurrentCrosshair.IsUser = isUser;
        CurrentCrosshair.WorkshopId = workshopId;
        CurrentCrosshair.ListedInWorkshop = listed;
        DraftColor = CurrentCrosshair.Color;
        SelectedPart = null;
        RebuildColorSlots();
        PersistSettings();
    }

    [RelayCommand]
    private void SaveCrosshair() => PersistSettings();

    [RelayCommand]
    private void OpenCreateDesign()
    {
        DraftName = string.IsNullOrWhiteSpace(CurrentCrosshair.Name) || CurrentCrosshair.Name == "Crosshair"
            ? Loc["designDefaultName"]
            : CurrentCrosshair.Name;
        DraftDescription = CurrentCrosshair.IsUser ? CurrentCrosshair.Description : "";
        DraftListed = CurrentCrosshair.IsUser && CurrentCrosshair.ListedInWorkshop;
        IsCreateOpen = true;
    }

    [RelayCommand]
    private void CancelCreateDesign() => IsCreateOpen = false;

    [RelayCommand]
    private void CreateDesign()
    {
        var name = string.IsNullOrWhiteSpace(DraftName) ? Loc["designDefaultName"] : DraftName.Trim();
        var design = CurrentCrosshair.Clone();
        design.Id = Guid.NewGuid().ToString("N")[..8];
        design.Name = name;
        design.Description = DraftDescription.Trim();
        design.IsUser = true;
        design.ListedInWorkshop = DraftListed;
        Customs.Add(design);
        RebuildDesignTiles();
        CurrentCrosshair = design;
        IsCreateOpen = false;
        PersistSettings();
        if (design.ListedInWorkshop)
            _ = PublishDesignAsync(design);
    }

    [RelayCommand]
    private void SelectDesign(DesignTile? tile)
    {
        if (tile == null)
            return;
        if (tile.IsAdd)
        {
            OpenCreateDesign();
            return;
        }

        if (tile.Design != null)
            CurrentCrosshair = tile.Design;
    }

    [RelayCommand]
    private void DeleteDesign()
    {
        if (!CurrentCrosshair.IsUser)
            return;
        Customs.Remove(CurrentCrosshair);
        RebuildDesignTiles();
        CurrentCrosshair = Customs.FirstOrDefault() ?? CreateDefault("Crosshair");
        PersistSettings();
    }

    [RelayCommand]
    private void ToggleOverlay()
    {
        if (IsOverlayVisible)
        {
            CloseOverlayEditor();
            _overlay.Hide();
            IsOverlayVisible = false;
        }
        else
        {
            PushOverlay();
            IsOverlayVisible = true;
        }

        OnPropertyChanged(nameof(OverlayButtonTitle));
        OnPropertyChanged(nameof(StatusText));
    }

    [RelayCommand]
    private void AddPart()
    {
        EnsureBoundDesign();
        var part = new CrosshairPart { Kind = DraftPartKind, Color = CurrentCrosshair.Color };
        CurrentCrosshair.ExtraParts.Add(part);
        SelectedPart = part;
        OnPropertyChanged(nameof(PartsBoundHint));
    }

    [RelayCommand]
    private void RemovePart()
    {
        if (SelectedPart == null)
            return;
        CurrentCrosshair.ExtraParts.Remove(SelectedPart);
        SelectedPart = CurrentCrosshair.ExtraParts.FirstOrDefault();
    }

    [RelayCommand]
    private void ToggleOverlayEditor()
    {
        if (IsOverlayEditorOpen)
            CloseOverlayEditor();
        else
            OpenOverlayEditor();
    }

    [RelayCommand]
    public void CloseOverlayEditor()
    {
        if (_closingEditor)
            return;
        _closingEditor = true;
        try
        {
            _overlay.SetEditMode(false);
            if (_editor != null)
            {
                _editor.Hide();
                NativeOverlay.Apply(_editor, clickThrough: true, alwaysOnTop: false, noActivate: true);
            }
            IsOverlayEditorOpen = false;
            if (IsOverlayVisible)
                PushOverlay();
            PersistSettings();
        }
        finally
        {
            _closingEditor = false;
        }
    }

    [RelayCommand]
    private void HideOverlaysFromEditor()
    {
        CloseOverlayEditor();
        if (IsOverlayVisible)
            ToggleOverlay();
    }

    [RelayCommand]
    private void BindOverlayHotkey()
    {
        IsCapturingHotkey = true;
        try
        {
            _hotkey.Suspend();
        }
        catch
        {
            // Capture still works from the studio window.
        }
        OnPropertyChanged(nameof(HotkeyButtonTitle));
    }

    [RelayCommand]
    private void ResetOverlayHotkey()
    {
        IsCapturingHotkey = false;
        OverlayHotkey.CopyFrom(HotkeyBinding.Default());
        OnPropertyChanged(nameof(HotkeyButtonTitle));
        OnPropertyChanged(nameof(OverlayEditorHint));
        try
        {
            BindHotkey();
        }
        catch
        {
        }
        PersistSettings();
    }

    [RelayCommand]
    private void OpenCreateWidget()
    {
        DraftWidgetName = Loc["widgetCustom"];
        DraftWidgetText = "";
        DraftWidgetListed = false;
        DraftWidgetFunction = WidgetKind.Template;
        DraftWidgetChrome = WidgetChrome.Glass;
        DraftWidgetClick = WidgetClickAction.None;
        DraftUseZoom = false;
        OnPropertyChanged(nameof(DraftFunctionChoice));
        OnPropertyChanged(nameof(DraftChromeChoice));
        OnPropertyChanged(nameof(DraftClickChoice));
        IsCreateWidgetOpen = true;
    }

    [RelayCommand]
    private void CancelCreateWidget() => IsCreateWidgetOpen = false;

    [RelayCommand]
    private void InsertDraftToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;
        DraftWidgetText = (DraftWidgetText ?? "") + token;
    }

    [RelayCommand]
    private void AddNote()
    {
        var index = Widgets.Count(w => w.Kind == WidgetKind.Notes);
        var widget = new HudWidget
        {
            Kind = WidgetKind.Notes,
            IsUser = true,
            Enabled = true,
            Chrome = WidgetChrome.Sticky,
            Color = index % 2 == 0 ? 0xFFFFE566 : 0xFF7DDA58,
            Text = "",
            NoteWidth = 220,
            NoteHeight = 168,
            PinnedLayout = true,
            LayoutX = 48 + index * 28,
            LayoutY = 120 + index * 28,
            Scale = 1,
            Opacity = 1
        };
        widget.AttachColors(ColorPresets);
        widget.PropertyChanged += OnWidgetChanged;
        Widgets.Add(widget);
        PersistSettings();
        if (IsOverlayVisible)
            PushOverlay();
    }

    [RelayCommand]
    private void CreateCustomWidget()
    {
        var widget = new HudWidget
        {
            Kind = WidgetKind.Custom,
            Name = string.IsNullOrWhiteSpace(DraftWidgetName) ? Loc["widgetCustom"] : DraftWidgetName.Trim(),
            Text = string.IsNullOrWhiteSpace(DraftWidgetText)
                ? (DraftWidgetFunction == WidgetKind.Template ? "{time}" : Loc["widgetCustomDefault"])
                : DraftWidgetText.Trim(),
            IsUser = true,
            Enabled = true,
            ListedInWorkshop = DraftWidgetListed,
            Function = DraftWidgetFunction,
            Chrome = DraftWidgetChrome,
            ClickAction = DraftWidgetClick,
            UseZoom = DraftUseZoom,
            Anchor = WidgetAnchor.Bottom,
            Color = 0xFFFFFFFF,
            Scale = 1
        };
        widget.AttachColors(ColorPresets);
        widget.PropertyChanged += OnWidgetChanged;
        Widgets.Add(widget);
        IsCreateWidgetOpen = false;
        PersistSettings();
        if (widget.ListedInWorkshop)
            _ = PublishWidgetAsync(widget);
        if (IsOverlayVisible)
            PushOverlay();
    }

    [RelayCommand]
    private void RemoveCustomWidget(HudWidget? widget)
    {
        if (widget is not { CanRemove: true })
            return;
        widget.PropertyChanged -= OnWidgetChanged;
        Widgets.Remove(widget);
        PersistSettings();
        if (IsOverlayVisible)
            PushOverlay();
    }

    [RelayCommand]
    private async Task ToggleDesignWorkshop()
    {
        if (!CurrentCrosshair.IsUser)
            return;
        await PublishDesignAsync(CurrentCrosshair);
        PersistSettings();
    }

    [RelayCommand]
    private async Task ToggleWidgetWorkshop(HudWidget? widget)
    {
        if (widget is not { IsCustom: true })
            return;
        await PublishWidgetAsync(widget);
        PersistSettings();
    }

    [RelayCommand]
    private async Task ExportDesign()
    {
        if (_host is not TopLevel top)
            return;
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Loc["exportTitle"],
            SuggestedFileName = (string.IsNullOrWhiteSpace(CurrentCrosshair.Name) ? "crosshair" : CurrentCrosshair.Name) + ".json",
            DefaultExtension = "json",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });
        if (file == null)
            return;
        await using var stream = await file.OpenWriteAsync();
        await JsonSerializer.SerializeAsync(stream, CurrentCrosshair, WorkshopClient.Json);
    }

    [RelayCommand]
    private async Task ImportDesign()
    {
        if (_host is not TopLevel top)
            return;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Loc["importTitle"],
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });
        var file = files.FirstOrDefault();
        if (file == null)
            return;
        await using var stream = await file.OpenReadAsync();
        var design = await JsonSerializer.DeserializeAsync<Crosshair>(stream, WorkshopClient.Json);
        if (design == null)
            return;
        AddWorkshopDesign(design, design.WorkshopId ?? "");
    }

    [RelayCommand]
    private void ToggleStopwatch(HudWidget widget) => widget.Running = !widget.Running;

    [RelayCommand]
    private void ToggleMagnifier(HudWidget widget) => widget.Running = !widget.Running;

    [RelayCommand]
    private void CenterMagnifier(HudWidget? widget)
    {
        if (widget == null || !widget.IsMagnifier)
            return;

        var monitor = SelectedMonitor ?? Monitors.FirstOrDefault();
        if (monitor == null)
            return;

        var capture = Math.Clamp(widget.CaptureSize <= 0 ? 88 : widget.CaptureSize, 32, 400);
        var zoom = Math.Clamp(widget.Zoom <= 0 ? 3 : widget.Zoom, 1.5, 8);
        var size = widget.OutputSize > 0
            ? Math.Clamp(widget.OutputSize, 80, 720)
            : (int)Math.Round(capture * zoom);
        widget.OutputSize = size;
        widget.LensPinned = true;
        widget.LensX = Math.Max(0, (monitor.Width - size) / 2);
        widget.LensY = Math.Max(0, (monitor.Height - size) / 2);
        if (!widget.Running && IsOverlayVisible)
            widget.Running = true;
    }

    [RelayCommand]
    private void BindWidgetKey(HudWidget? widget)
    {
        if (widget == null)
            return;
        if (_bindingWidget != null)
            _bindingWidget.IsBindingKey = false;
        _bindingWidget = widget;
        widget.IsBindingKey = true;
        try
        {
            _hotkey.Suspend();
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void ClearWidgetKey(HudWidget? widget)
    {
        if (widget == null)
            return;
        if (ReferenceEquals(_bindingWidget, widget))
            StopBindingKey();
        widget.TriggerVirtualKey = 0;
        widget.TriggerKeyName = "";
        widget.TriggerCtrl = false;
        widget.TriggerShift = false;
        widget.TriggerAlt = false;
    }

    [RelayCommand]
    private async Task BrowseWidgetImage(HudWidget? widget)
    {
        if (widget == null || _host is not TopLevel top)
            return;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Loc["widgetImage"],
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }
            ]
        });
        var file = files.FirstOrDefault();
        if (file == null)
            return;
        widget.ImagePath = file.Path.LocalPath;
    }

    [RelayCommand]
    private void ClearWidgetImage(HudWidget? widget)
    {
        if (widget != null)
            widget.ImagePath = "";
    }

    [RelayCommand]
    private void ResetStopwatch(HudWidget widget)
    {
        widget.Running = false;
        _overlay.ResetStopwatch(widget.Id);
    }

    [RelayCommand]
    private void ToggleCountdown(HudWidget widget) => widget.Running = !widget.Running;

    [RelayCommand]
    private void NudgeCounter(HudWidget widget) => widget.Count++;

    [RelayCommand]
    private void DropCounter(HudWidget widget) => widget.Count = Math.Max(0, widget.Count - 1);

    [RelayCommand]
    private void ResetCounter(HudWidget widget) => widget.Count = 0;

    [RelayCommand]
    private void NudgeScoreLeft(HudWidget widget) => widget.ScoreLeft++;

    [RelayCommand]
    private void DropScoreLeft(HudWidget widget) => widget.ScoreLeft = Math.Max(0, widget.ScoreLeft - 1);

    [RelayCommand]
    private void NudgeScoreRight(HudWidget widget) => widget.ScoreRight++;

    [RelayCommand]
    private void DropScoreRight(HudWidget widget) => widget.ScoreRight = Math.Max(0, widget.ScoreRight - 1);

    [RelayCommand]
    private void ResetScore(HudWidget widget)
    {
        widget.ScoreLeft = 0;
        widget.ScoreRight = 0;
    }

    public void Shutdown()
    {
        CloseOverlayEditor();
        StopBindingKey();
        _input.Changed -= OnGlobalInput;
        _input.Dispose();
        if (_editor != null)
        {
            _editor.Closing -= OnEditorClosing;
            _editor.Close();
            _editor = null;
        }
        _hotkey.Dispose();
        _overlay.Hide();
        PersistSettings();
    }

    private void PushOverlay()
    {
        var monitor = SelectedMonitor ?? Monitors.FirstOrDefault() ?? new MonitorInfo { Width = 1920, Height = 1080, Scaling = 1, IsPrimary = true };
        _overlay.Show(CurrentCrosshair, Widgets, Display, monitor, Display.ShowCrosshairOverlay);
        if (IsOverlayEditorOpen)
            _overlay.SetEditMode(true);
    }

    public bool HandleKey(Key key, KeyModifiers modifiers)
    {
        if (_bindingWidget != null)
        {
            CaptureWidgetKey(key, modifiers);
            return true;
        }

        if (IsCapturingHotkey)
        {
            CaptureHotkey(key, modifiers);
            return true;
        }

        if (key == Key.Escape && IsOverlayEditorOpen)
        {
            CloseOverlayEditor();
            return true;
        }

        if (!_hotkey.IsRegistered && MatchesOverlayHotkey(key, modifiers))
        {
            ToggleOverlayEditor();
            return true;
        }

        return false;
    }

    public void CaptureHotkey(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Escape)
        {
            IsCapturingHotkey = false;
            BindHotkey();
            OnPropertyChanged(nameof(HotkeyButtonTitle));
            return;
        }

        if (HotkeyMap.IsModifier(key))
            return;

        var vk = HotkeyMap.ToVirtualKey(key);
        if (vk == 0)
            return;

        var ctrl = modifiers.HasFlag(KeyModifiers.Control);
        var shift = modifiers.HasFlag(KeyModifiers.Shift);
        var alt = modifiers.HasFlag(KeyModifiers.Alt);
        var win = modifiers.HasFlag(KeyModifiers.Meta);
        var function = key is >= Key.F1 and <= Key.F24;
        if (!ctrl && !shift && !alt && !win && !function)
            return;

        OverlayHotkey.Ctrl = ctrl;
        OverlayHotkey.Shift = shift;
        OverlayHotkey.Alt = alt;
        OverlayHotkey.Win = win;
        OverlayHotkey.VirtualKey = vk;
        OverlayHotkey.KeyName = HotkeyMap.ToName(key);
        OverlayHotkey.RefreshDisplay();
        IsCapturingHotkey = false;
        OnPropertyChanged(nameof(HotkeyButtonTitle));
        OnPropertyChanged(nameof(OverlayEditorHint));
        try
        {
            BindHotkey();
        }
        catch
        {
        }
        PersistSettings();
    }

    private void CaptureWidgetKey(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Escape)
        {
            StopBindingKey();
            return;
        }

        if (HotkeyMap.IsModifier(key))
            return;

        var vk = HotkeyMap.ToVirtualKey(key);
        if (vk == 0 || _bindingWidget == null)
            return;

        ApplyWidgetKey(_bindingWidget, vk,
            modifiers.HasFlag(KeyModifiers.Control),
            modifiers.HasFlag(KeyModifiers.Shift),
            modifiers.HasFlag(KeyModifiers.Alt));
    }

    private void ApplyWidgetKey(HudWidget widget, int vk, bool ctrl, bool shift, bool alt)
    {
        widget.TriggerVirtualKey = vk;
        widget.TriggerKeyName = HotkeyMap.FromVirtualKey(vk);
        widget.TriggerCtrl = ctrl;
        widget.TriggerShift = shift;
        widget.TriggerAlt = alt;
        StopBindingKey();
        PersistSettings();
    }

    private void StopBindingKey()
    {
        if (_bindingWidget != null)
            _bindingWidget.IsBindingKey = false;
        _bindingWidget = null;
        BindHotkey();
    }

    private void OnGlobalInput(int vk, bool down)
    {
        if (InputWatch.IsModifierVk(vk))
            return;

        if (_bindingWidget != null)
        {
            if (down)
                ApplyWidgetKey(_bindingWidget, vk, InputWatch.CtrlDown(), InputWatch.ShiftDown(), InputWatch.AltDown());
            return;
        }

        if (!IsOverlayVisible)
            return;

        var ctrl = InputWatch.CtrlDown();
        var shift = InputWatch.ShiftDown();
        var alt = InputWatch.AltDown();
        foreach (var widget in Widgets)
        {
            if (!widget.Enabled || !widget.MatchesTrigger(vk, ctrl, shift, alt))
                continue;

            if (widget.IsMagnifier)
            {
                if (widget.ZoomTrigger == ZoomTrigger.KeyHold)
                    widget.Running = down;
                else if (down)
                    widget.Running = !widget.Running;
            }
            else if (widget.IsTemplate && down)
                HudWindow.ApplyClick(widget);
        }
    }

    public bool MatchesOverlayHotkey(Key key, KeyModifiers modifiers)
        => HotkeyMap.ToVirtualKey(key) == OverlayHotkey.VirtualKey
           && modifiers.HasFlag(KeyModifiers.Control) == OverlayHotkey.Ctrl
           && modifiers.HasFlag(KeyModifiers.Shift) == OverlayHotkey.Shift
           && modifiers.HasFlag(KeyModifiers.Alt) == OverlayHotkey.Alt
           && modifiers.HasFlag(KeyModifiers.Meta) == OverlayHotkey.Win;

    private void OpenOverlayEditor()
    {
        if (!IsOverlayVisible)
        {
            PushOverlay();
            IsOverlayVisible = true;
            OnPropertyChanged(nameof(OverlayButtonTitle));
            OnPropertyChanged(nameof(StatusText));
        }

        _overlay.SetEditMode(true);
        var monitor = SelectedMonitor ?? Monitors.FirstOrDefault() ?? new MonitorInfo { Width = 1920, Height = 1080, Scaling = 1, IsPrimary = true };
        _editor ??= new OverlayEditorWindow { DataContext = this };
        _editor.Closing -= OnEditorClosing;
        _editor.Closing += OnEditorClosing;
        if (!_editor.IsVisible)
            _editor.Show();
        _editor.Place(monitor);
        IsOverlayEditorOpen = true;
        PushOverlay();
    }

    private void OnEditorClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closingEditor)
            return;
        e.Cancel = true;
        CloseOverlayEditor();
    }

    private void BindHotkey()
    {
        try
        {
            if (IsCapturingHotkey || _bindingWidget != null)
            {
                _hotkey.Suspend();
                return;
            }

            _hotkey.Start(OverlayHotkey.Modifiers, (uint)OverlayHotkey.VirtualKey, () =>
            {
                if (IsCapturingHotkey)
                    return;
                ToggleOverlayEditor();
            });
        }
        catch
        {
            // Studio key handling still works when the global hook cannot bind.
        }
    }

    public void AddWorkshopDesign(Crosshair design, string workshopId)
    {
        design.ExtraParts ??= [];
        design.Id = Guid.NewGuid().ToString("N")[..8];
        design.IsUser = true;
        design.WorkshopId = string.IsNullOrWhiteSpace(workshopId) ? null : workshopId;
        design.ListedInWorkshop = false;
        if (string.IsNullOrWhiteSpace(design.Name))
            design.Name = Loc["designDefaultName"];
        Customs.Add(design);
        RebuildDesignTiles();
        CurrentCrosshair = design;
        Workspace = "Crosshair";
        PersistSettings();
    }

    public void AddWorkshopWidget(HudWidget widget, string workshopId)
    {
        widget.Id = Guid.NewGuid().ToString("N")[..8];
        widget.Kind = WidgetKind.Custom;
        widget.IsUser = true;
        widget.WorkshopId = string.IsNullOrWhiteSpace(workshopId) ? null : workshopId;
        widget.ListedInWorkshop = false;
        widget.Enabled = true;
        if (widget.Scale <= 0)
            widget.Scale = 1;
        if (string.IsNullOrWhiteSpace(widget.Name))
            widget.Name = Loc["widgetCustom"];
        widget.AttachColors(ColorPresets);
        widget.PropertyChanged += OnWidgetChanged;
        Widgets.Add(widget);
        Workspace = "Widgets";
        PersistSettings();
        if (IsOverlayVisible)
            PushOverlay();
    }

    private async Task PublishDesignAsync(Crosshair design)
    {
        try
        {
            var published = await _workshop.PublishAsync(
                "crosshair",
                design.WorkshopId,
                design.Name,
                design.Description,
                design.ListedInWorkshop,
                design);
            if (published == null)
                return;
            design.WorkshopId = published.Id;
            design.ListedInWorkshop = published.Listed;
        }
        catch
        {
            Workshop.Status = Loc["workshopOffline"];
        }
    }

    private async Task PublishWidgetAsync(HudWidget widget)
    {
        try
        {
            var published = await _workshop.PublishAsync(
                "widget",
                widget.WorkshopId,
                widget.Title,
                widget.Text,
                widget.ListedInWorkshop,
                widget);
            if (published == null)
                return;
            widget.WorkshopId = published.Id;
            widget.ListedInWorkshop = published.Listed;
        }
        catch
        {
            Workshop.Status = Loc["workshopOffline"];
        }
    }

    private void EnsureBoundDesign()
    {
        if (CurrentCrosshair.IsUser && Customs.Any(c => ReferenceEquals(c, CurrentCrosshair)))
            return;

        var design = CurrentCrosshair;
        design.IsUser = true;
        if (string.IsNullOrWhiteSpace(design.Id))
            design.Id = Guid.NewGuid().ToString("N")[..8];
        if (string.IsNullOrWhiteSpace(design.Name) || design.Name is "Crosshair" or "New Crosshair")
            design.Name = Loc["designDefaultName"];
        if (!Customs.Any(c => ReferenceEquals(c, design)))
            Customs.Add(design);
        RebuildDesignTiles();
        RefreshDesignSelection();
        OnPropertyChanged(nameof(IsUserDesign));
        OnPropertyChanged(nameof(PartsBoundHint));
    }

    private void PersistSettings()
    {
        _appSettings.Widgets = Widgets.ToList();
        _appSettings.Library = Customs.ToList();
        _appSettings.Crosshair = CurrentCrosshair;
        _appSettings.OverlayHotkey = OverlayHotkey;
        _store.SaveSettings(_appSettings);
    }

    private static bool TryColor(object? value, out uint color)
    {
        switch (value)
        {
            case ColorSwatch swatch:
                color = swatch.Value;
                return true;
            case uint u:
                color = u;
                return true;
            case int i:
                color = unchecked((uint)i);
                return true;
            case long l:
                color = unchecked((uint)l);
                return true;
            default:
                color = 0;
                return false;
        }
    }

    private void OnLanguageChanged()
    {
        foreach (var shape in Shapes)
            shape.RefreshTitle();
        foreach (var widget in Widgets)
            widget.RefreshLoc();
        foreach (var slot in ColorSlots)
            slot.Refresh();
        RebuildColorSlots();
        Onboarding.Refresh();
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(OverlayButtonTitle));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(IsUserDesign));
        OnPropertyChanged(nameof(PartsBoundHint));
        OnPropertyChanged(nameof(HotkeyButtonTitle));
        OnPropertyChanged(nameof(OverlayEditorHint));
        OnPropertyChanged(nameof(ChromeOptions));
        OnPropertyChanged(nameof(ClickOptions));
        OnPropertyChanged(nameof(ZoomTriggerOptions));
        OnPropertyChanged(nameof(ZoomSourceOptions));
        OnPropertyChanged(nameof(WidgetFunctions));
        foreach (var widget in Widgets)
            widget.ColorSlot?.Refresh();
    }

    private void Hook(Crosshair crosshair)
    {
        crosshair.PropertyChanged += OnProfileChanged;
        crosshair.ExtraParts.CollectionChanged += OnExtraPartsChanged;
        foreach (var part in crosshair.ExtraParts)
            part.PropertyChanged += OnPartChanged;
    }

    private void Unhook(Crosshair? crosshair)
    {
        if (crosshair == null)
            return;
        crosshair.PropertyChanged -= OnProfileChanged;
        crosshair.ExtraParts.CollectionChanged -= OnExtraPartsChanged;
        foreach (var part in crosshair.ExtraParts)
            part.PropertyChanged -= OnPartChanged;
    }

    private void OnExtraPartsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CrosshairPart part in e.NewItems)
                part.PropertyChanged += OnPartChanged;
        }

        if (e.OldItems != null)
        {
            foreach (CrosshairPart part in e.OldItems)
                part.PropertyChanged -= OnPartChanged;
        }

        OnCrosshairChanged();
        RebuildColorSlots();
        SchedulePersist();
    }

    private void OnPartChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CrosshairPart.Color))
            SyncColorSlots();
        OnCrosshairChanged();
        SchedulePersist();
    }

    private void OnWidgetChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is HudWidget widget && !_layoutSync)
        {
            if (e.PropertyName is nameof(HudWidget.Anchor) or nameof(HudWidget.OffsetX) or nameof(HudWidget.OffsetY))
            {
                if (widget.PinnedLayout)
                {
                    _layoutSync = true;
                    widget.PinnedLayout = false;
                    _layoutSync = false;
                }
            }
            else if (e.PropertyName is nameof(HudWidget.LayoutX) or nameof(HudWidget.LayoutY))
            {
                if (!widget.PinnedLayout)
                {
                    _layoutSync = true;
                    widget.PinnedLayout = true;
                    _layoutSync = false;
                }
            }
            else if (e.PropertyName == nameof(HudWidget.Color))
                widget.ColorSlot?.Sync();
            else if (e.PropertyName is nameof(HudWidget.Name) or nameof(HudWidget.Text))
                widget.RefreshLoc();
            else if (e.PropertyName is nameof(HudWidget.LensX) or nameof(HudWidget.LensY))
                widget.LensPinned = true;
            else if (e.PropertyName is nameof(HudWidget.SourceX) or nameof(HudWidget.SourceY))
                widget.ZoomSource = ZoomSource.Pinned;
            else if (e.PropertyName == nameof(HudWidget.ListedInWorkshop) && widget.IsCustom)
                _ = PublishWidgetAsync(widget);
        }

        if (e.PropertyName == nameof(HudWidget.Text) && sender is HudWidget notes && notes.IsNotes && IsOverlayEditorOpen)
        {
            SchedulePersist();
            return;
        }

        if (IsOverlayVisible)
            PushOverlay();
        SchedulePersist();
    }

    private void OnProfileChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Crosshair.Type) or nameof(Crosshair.ShowCenterDot)
            or nameof(Crosshair.ShowOutline) or nameof(Crosshair.ShowInnerCircle)
            or nameof(Crosshair.ShowOuterRing) or nameof(Crosshair.ShowGlow)
            or nameof(Crosshair.ShowCrosshairLines) or nameof(Crosshair.ShowMidDots)
            or nameof(Crosshair.ShowSideBrackets))
        {
            NotifyFlags();
            RebuildColorSlots();
            if (e.PropertyName == nameof(Crosshair.Type))
                OnPropertyChanged(nameof(SelectedShape));
        }

        if (e.PropertyName is nameof(Crosshair.Color) or nameof(Crosshair.DotColor) or nameof(Crosshair.LineColor)
            or nameof(Crosshair.OutlineColor) or nameof(Crosshair.InnerCircleColor)
            or nameof(Crosshair.OuterRingColor) or nameof(Crosshair.GlowColor))
            SyncColorSlots();

        if (e.PropertyName is nameof(Crosshair.Name) or nameof(Crosshair.Description) or nameof(Crosshair.IsUser))
        {
            OnPropertyChanged(nameof(IsUserDesign));
            OnPropertyChanged(nameof(PartsBoundHint));
        }

        if (e.PropertyName == nameof(Crosshair.ListedInWorkshop) && CurrentCrosshair.IsUser)
            _ = PublishDesignAsync(CurrentCrosshair);

        OnCrosshairChanged();
        SchedulePersist();
    }

    private void SchedulePersist()
    {
        _persist ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _persist.Tick -= OnPersistTick;
        _persist.Tick += OnPersistTick;
        _persist.Stop();
        _persist.Start();
    }

    private void OnPersistTick(object? sender, EventArgs e)
    {
        _persist?.Stop();
        PersistSettings();
    }

    private void NotifyFlags()
    {
        OnPropertyChanged(nameof(ShowArmToggles));
        OnPropertyChanged(nameof(ShowCustomMetrics));
        OnPropertyChanged(nameof(ShowFill));
        OnPropertyChanged(nameof(ShowGap));
        OnPropertyChanged(nameof(ShowTicksOption));
        OnPropertyChanged(nameof(ShowArcOption));
        OnPropertyChanged(nameof(ShowCornerRadius));
        OnPropertyChanged(nameof(ShowDotOptions));
    }

    private void OnCrosshairChanged()
    {
        if (IsOverlayVisible)
            PushOverlay();
    }

    private static ShapeOption Shape(string key, CrosshairType type) => new()
    {
        Type = type,
        TitleKey = key,
        Preview = new Crosshair
        {
            Type = type,
            Color = 0xFFF5F5F7,
            Size = 18,
            Thickness = 1.6,
            Gap = 3,
            ShowOutline = true,
            OutlineColor = 0xFF000000,
            ShowCenterDot = type == CrosshairType.Circle,
            DotSize = 2,
            ArcDegrees = type == CrosshairType.Arc ? 240 : 360
        }
    };

    private static Crosshair CreateDefault(string name) => new()
    {
        Name = name,
        Type = CrosshairType.Cross,
        Color = 0xFFFFFFFF,
        Size = 18,
        Thickness = 2,
        Gap = 3,
        ShowOutline = true,
        OutlineColor = 0xFF000000,
        OutlineThickness = 2
    };

    private void RebuildDesignTiles()
    {
        DesignTiles.Clear();
        foreach (var design in Customs)
            DesignTiles.Add(new DesignTile { Design = design, IsSelected = design.Id == CurrentCrosshair.Id });
        DesignTiles.Add(new DesignTile { IsAdd = true });
    }

    private void RefreshDesignSelection()
    {
        foreach (var tile in DesignTiles)
            tile.IsSelected = tile.Design != null && tile.Design.Id == CurrentCrosshair.Id && CurrentCrosshair.IsUser;
    }

    private void RebuildColorSlots()
    {
        ColorSlots.Clear();
        var ch = CurrentCrosshair;
        ColorSlots.Add(new ColorSlot("colorShape", ColorPresets, () => ch.Color, c =>
        {
            DraftColor = c;
            ch.Color = c;
        }));
        if (ch.Type == CrosshairType.Custom)
            ColorSlots.Add(new ColorSlot("colorLines", ColorPresets, () => ch.LineColor, c => ch.LineColor = c));

        ColorSlots.Add(new ColorSlot("colorDot", ColorPresets, () => ch.DotColor, c => ch.DotColor = c));
        ColorSlots.Add(new ColorSlot("colorOutline", OutlinePresets, () => ch.OutlineColor, c => ch.OutlineColor = c));
        ColorSlots.Add(new ColorSlot("colorInner", ColorPresets, () => ch.InnerCircleColor, c => ch.InnerCircleColor = c));
        ColorSlots.Add(new ColorSlot("colorOuter", ColorPresets, () => ch.OuterRingColor, c => ch.OuterRingColor = c));
        ColorSlots.Add(new ColorSlot("colorGlow", ColorPresets, () => ch.GlowColor, c => ch.GlowColor = c));

        foreach (var part in ch.ExtraParts)
        {
            var captured = part;
            ColorSlots.Add(new ColorSlot("", ColorPresets, () => captured.Color, c => captured.Color = c, captured.Title));
        }

        OnPropertyChanged(nameof(ShapeColorSlot));
        RefreshPartColorSlot();
    }

    private void RefreshPartColorSlot()
    {
        var part = SelectedPart;
        SelectedPartColorSlot = part == null
            ? null
            : new ColorSlot("", ColorPresets, () => part.Color, c => part.Color = c, part.Title);
        OnPropertyChanged(nameof(SelectedPartColorSlot));
    }

    private void SyncColorSlots()
    {
        foreach (var slot in ColorSlots)
            slot.Sync();
        SelectedPartColorSlot?.Sync();
        ShapeColorSlot?.Sync();
    }

    private static List<HudWidget> CreateDefaultWidgets() =>
    [
        new() { Kind = WidgetKind.Clock, Anchor = WidgetAnchor.TopRight, Enabled = true },
        new() { Kind = WidgetKind.Date, Anchor = WidgetAnchor.TopRight, OffsetY = 44 },
        new() { Kind = WidgetKind.Session, Anchor = WidgetAnchor.TopLeft, Enabled = true },
        new() { Kind = WidgetKind.Uptime, Anchor = WidgetAnchor.TopLeft, OffsetY = 44 },
        new() { Kind = WidgetKind.Stopwatch, Anchor = WidgetAnchor.Top },
        new() { Kind = WidgetKind.Countdown, Anchor = WidgetAnchor.Top, OffsetY = 44, CountdownSeconds = 40 },
        new() { Kind = WidgetKind.Counter, Anchor = WidgetAnchor.Top, OffsetX = -140, Text = "K" },
        new() { Kind = WidgetKind.Score, Anchor = WidgetAnchor.Top, OffsetX = 140 },
        new() { Kind = WidgetKind.System, Anchor = WidgetAnchor.BottomRight, Enabled = true },
        new() { Kind = WidgetKind.Cpu, Anchor = WidgetAnchor.BottomRight, OffsetY = 44 },
        new() { Kind = WidgetKind.Ram, Anchor = WidgetAnchor.BottomRight, OffsetY = 88 },
        new() { Kind = WidgetKind.Network, Anchor = WidgetAnchor.BottomRight, OffsetY = 132 },
        new() { Kind = WidgetKind.Ping, Anchor = WidgetAnchor.BottomLeft },
        new() { Kind = WidgetKind.Battery, Anchor = WidgetAnchor.BottomLeft, OffsetY = 44 },
        new() { Kind = WidgetKind.Disk, Anchor = WidgetAnchor.BottomLeft, OffsetY = 88 },
        new() { Kind = WidgetKind.Display, Anchor = WidgetAnchor.Bottom, OffsetY = 44 },
        new() { Kind = WidgetKind.ActiveApp, Anchor = WidgetAnchor.Bottom, OffsetY = 88 },
        new() { Kind = WidgetKind.Notes, Anchor = WidgetAnchor.Bottom, Text = "Shift · Ctrl · F", Enabled = true, Color = 0xFFFFE566, Chrome = WidgetChrome.Sticky, NoteWidth = 220, NoteHeight = 168 },
        new() { Kind = WidgetKind.Magnifier, Anchor = WidgetAnchor.BottomRight, OffsetX = -160, Zoom = 3, CaptureSize = 88, OutputSize = 240 }
    ];
}
