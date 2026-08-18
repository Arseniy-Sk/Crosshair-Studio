using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosshairStudio.Infrastructure.Localization;

namespace CrosshairStudio.Domain;

public partial class DisplaySettings : ObservableObject
{
    [ObservableProperty] private int _selectedMonitorIndex;
    [ObservableProperty] private bool _alwaysOnTop = true;
    [ObservableProperty] private bool _clickThrough = true;
    [ObservableProperty] private int _offsetX;
    [ObservableProperty] private int _offsetY;
    [ObservableProperty] private bool _usePhysicalPixels = true;
    [ObservableProperty] private bool _showCrosshairOverlay = true;
}

public partial class AppSettings : ObservableObject
{
    [ObservableProperty] private bool _onboardingCompleted;
    [ObservableProperty] private string _language = Loc.GuessLanguage();
    public DisplaySettings Display { get; set; } = new();
    public List<HudWidget> Widgets { get; set; } = [];
    public Crosshair? Crosshair { get; set; }
    public List<Crosshair> Library { get; set; } = [];
    public HotkeyBinding OverlayHotkey { get; set; } = HotkeyBinding.Default();
    public string ClientId { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "Player";
    public string WorkshopUrl { get; set; } = "http://150.251.152.203:8787";
}

public sealed class MonitorInfo
{
    public int Index { get; init; }
    public string Name { get; init; } = "Display";
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public double Scaling { get; init; } = 1;
    public bool IsPrimary { get; init; }

    public bool IsPortrait => Height > Width;
    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;
    public int ScalePercent => (int)Math.Round(Scaling * 100);

    public override string ToString()
    {
        var orientation = IsPortrait ? "Portrait" : "Landscape";
        return $"{Name} · {Width}×{Height} · {ScalePercent}% · {orientation}";
    }
}

public sealed class ShapeOption : ObservableObject
{
    public CrosshairType Type { get; init; }
    public string TitleKey { get; init; } = string.Empty;
    public Crosshair Preview { get; init; } = new();
    public string Title => Loc.Current[TitleKey];
    public void RefreshTitle() => OnPropertyChanged(nameof(Title));
}

public sealed record PartKindOption(OverlayPartKind Kind, string TitleKey)
{
    public string Title => Loc.Current[TitleKey];
}

public sealed class ColorSwatch
{
    public uint Value { get; init; }
}

public sealed partial class ColorSwatchItem : ObservableObject
{
    public uint Value { get; init; }
    [ObservableProperty] private bool _isSelected;
    public IBrush SelectionBrush => IsSelected
        ? new SolidColorBrush(Colors.White)
        : Brushes.Transparent;

    partial void OnIsSelectedChanged(bool value) => OnPropertyChanged(nameof(SelectionBrush));
}

public sealed partial class ColorSlot : ObservableObject
{
    private readonly Action<uint> _apply;
    private readonly Func<uint> _get;
    private bool _syncing;

    public ColorSlot(string titleKey, IReadOnlyList<ColorSwatch> presets, Func<uint> get, Action<uint> apply, string caption = "")
    {
        TitleKey = titleKey;
        Caption = caption;
        _get = get;
        _apply = apply;
        foreach (var preset in presets)
            Swatches.Add(new ColorSwatchItem { Value = preset.Value });
        SetCurrent(get(), apply: false);
    }

    public string TitleKey { get; }
    public string Caption { get; }
    public ObservableCollection<ColorSwatchItem> Swatches { get; } = [];
    public string Title => string.IsNullOrEmpty(TitleKey) ? Caption : Loc.Current[TitleKey];

    [ObservableProperty] private uint _current;
    [ObservableProperty] private string _hex = "FFFFFF";
    [ObservableProperty] private double _red = 255;
    [ObservableProperty] private double _green = 255;
    [ObservableProperty] private double _blue = 255;
    [ObservableProperty] private bool _isPickerOpen;

    public void Refresh() => OnPropertyChanged(nameof(Title));

    public void Sync()
    {
        var color = _get();
        if (color != Current)
            SetCurrent(color, apply: false);
        else
            RefreshSwatches();
    }

    [RelayCommand]
    private void Pick(object? value)
    {
        if (!TryColor(value, out var color))
            return;
        SetCurrent(color, apply: true);
    }

    [RelayCommand]
    private void TogglePicker() => IsPickerOpen = !IsPickerOpen;

    public void CommitHex()
    {
        var text = Hex.Trim().TrimStart('#');
        if (text.Length == 6 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            SetCurrent(0xFF000000 | rgb, apply: true);
        else if (text.Length == 8 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            SetCurrent(argb, apply: true);
        else
            Hex = FormatHex(Current);
    }

    partial void OnRedChanged(double value)
    {
        if (!_syncing)
            Compose();
    }

    partial void OnGreenChanged(double value)
    {
        if (!_syncing)
            Compose();
    }

    partial void OnBlueChanged(double value)
    {
        if (!_syncing)
            Compose();
    }

    private void Compose()
    {
        var color = 0xFF000000u
                    | ((uint)Math.Clamp(Red, 0, 255) << 16)
                    | ((uint)Math.Clamp(Green, 0, 255) << 8)
                    | (uint)Math.Clamp(Blue, 0, 255);
        SetCurrent(color, apply: true);
    }

    private void SetCurrent(uint color, bool apply)
    {
        _syncing = true;
        Current = color;
        Hex = FormatHex(color);
        Red = (color >> 16) & 0xFF;
        Green = (color >> 8) & 0xFF;
        Blue = color & 0xFF;
        _syncing = false;
        RefreshSwatches();
        if (apply)
            _apply(color);
    }

    private void RefreshSwatches()
    {
        foreach (var swatch in Swatches)
            swatch.IsSelected = swatch.Value == Current;
    }

    private static string FormatHex(uint color) => $"{color & 0xFFFFFF:X6}";

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
            default:
                color = 0;
                return false;
        }
    }
}

public sealed partial class DesignTile : ObservableObject
{
    public bool IsAdd { get; init; }
    public Crosshair? Design { get; init; }
    [ObservableProperty] private bool _isSelected;
}

public sealed partial class HotkeyBinding : ObservableObject
{
    [ObservableProperty] private bool _ctrl;
    [ObservableProperty] private bool _shift = true;
    [ObservableProperty] private bool _alt;
    [ObservableProperty] private bool _win;
    [ObservableProperty] private int _virtualKey = 0xC0;
    [ObservableProperty] private string _keyName = "`";

    public uint Modifiers
        => (Alt ? 1u : 0) | (Ctrl ? 2u : 0) | (Shift ? 4u : 0) | (Win ? 8u : 0);

    public string Display
    {
        get
        {
            var parts = new List<string>();
            if (Ctrl) parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt) parts.Add("Alt");
            if (Win) parts.Add("Win");
            parts.Add(string.IsNullOrWhiteSpace(KeyName) ? "?" : KeyName);
            return string.Join(" + ", parts);
        }
    }

    partial void OnCtrlChanged(bool value) => RefreshDisplay();
    partial void OnShiftChanged(bool value) => RefreshDisplay();
    partial void OnAltChanged(bool value) => RefreshDisplay();
    partial void OnWinChanged(bool value) => RefreshDisplay();
    partial void OnKeyNameChanged(string value) => RefreshDisplay();

    public void RefreshDisplay() => OnPropertyChanged(nameof(Display));

    public static HotkeyBinding Default() => new();

    public void CopyFrom(HotkeyBinding other)
    {
        Ctrl = other.Ctrl;
        Shift = other.Shift;
        Alt = other.Alt;
        Win = other.Win;
        VirtualKey = other.VirtualKey;
        KeyName = other.KeyName;
        RefreshDisplay();
    }
}

public sealed class OnboardingPage
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

