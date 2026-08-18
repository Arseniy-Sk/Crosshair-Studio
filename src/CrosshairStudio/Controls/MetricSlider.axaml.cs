using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CrosshairStudio.Controls;

public partial class MetricSlider : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MetricSlider, string>(nameof(Label), "");

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MetricSlider, double>(nameof(Value), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MetricSlider, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MetricSlider, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> DefaultProperty =
        AvaloniaProperty.Register<MetricSlider, double>(nameof(Default));

    public static readonly StyledProperty<int> DecimalsProperty =
        AvaloniaProperty.Register<MetricSlider, int>(nameof(Decimals));

    public static readonly StyledProperty<string> SuffixProperty =
        AvaloniaProperty.Register<MetricSlider, string>(nameof(Suffix), "");

    public static readonly StyledProperty<bool> IsPercentProperty =
        AvaloniaProperty.Register<MetricSlider, bool>(nameof(IsPercent));

    public static readonly DirectProperty<MetricSlider, bool> IsEditingProperty =
        AvaloniaProperty.RegisterDirect<MetricSlider, bool>(nameof(IsEditing), o => o.IsEditing);

    public static readonly DirectProperty<MetricSlider, string> DisplayTextProperty =
        AvaloniaProperty.RegisterDirect<MetricSlider, string>(nameof(DisplayText), o => o.DisplayText);

    private bool _isEditing;
    private string _displayText = "";

    static MetricSlider()
    {
        ValueProperty.Changed.AddClassHandler<MetricSlider>((s, _) => s.RefreshText());
        DecimalsProperty.Changed.AddClassHandler<MetricSlider>((s, _) => s.RefreshText());
        SuffixProperty.Changed.AddClassHandler<MetricSlider>((s, _) => s.RefreshText());
        IsPercentProperty.Changed.AddClassHandler<MetricSlider>((s, _) => s.RefreshText());
    }

    public MetricSlider()
    {
        InitializeComponent();
        RefreshText();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Default
    {
        get => GetValue(DefaultProperty);
        set => SetValue(DefaultProperty, value);
    }

    public int Decimals
    {
        get => GetValue(DecimalsProperty);
        set => SetValue(DecimalsProperty, value);
    }

    public string Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public bool IsPercent
    {
        get => GetValue(IsPercentProperty);
        set => SetValue(IsPercentProperty, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set => SetAndRaise(IsEditingProperty, ref _isEditing, value);
    }

    public string DisplayText
    {
        get => _displayText;
        private set => SetAndRaise(DisplayTextProperty, ref _displayText, value);
    }

    private void Slider_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        Value = Math.Clamp(Default, Minimum, Maximum);
        e.Handled = true;
    }

    private async void Value_OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        EditBox.Text = EditSource();
        IsEditing = true;
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            EditBox.Focus();
            EditBox.SelectAll();
        });
        e.Handled = true;
    }

    private void Edit_OnLostFocus(object? sender, RoutedEventArgs e) => CommitEdit();

    private void Edit_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitEdit();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            IsEditing = false;
            e.Handled = true;
        }
    }

    private void CommitEdit()
    {
        if (!IsEditing)
            return;
        IsEditing = false;
        var raw = (EditBox.Text ?? "").Trim().TrimEnd('%', '°', ' ');
        raw = raw.Replace(',', '.');
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            && !double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
            return;

        if (IsPercent && parsed > 1)
            parsed /= 100.0;

        Value = Math.Clamp(parsed, Minimum, Maximum);
    }

    private string EditSource()
    {
        if (IsPercent)
            return Math.Round(Value * 100).ToString("0", CultureInfo.InvariantCulture);
        var format = Decimals <= 0 ? "0" : "0." + new string('#', Decimals);
        return Value.ToString(format, CultureInfo.InvariantCulture);
    }

    private void RefreshText()
    {
        if (IsPercent)
        {
            DisplayText = $"{Math.Round(Value * 100):0}%";
            return;
        }

        var text = Value.ToString("F" + Math.Clamp(Decimals, 0, 4), CultureInfo.CurrentCulture);
        DisplayText = string.IsNullOrEmpty(Suffix) ? text : text + Suffix;
    }
}
