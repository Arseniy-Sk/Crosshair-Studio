using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosshairStudio.Domain;
using CrosshairStudio.Infrastructure.Localization;

namespace CrosshairStudio.ViewModels;

public partial class OnboardingViewModel : ViewModelBase
{
    private readonly CrosshairType[] _tourTypes =
    [
        CrosshairType.Cross, CrosshairType.Circle, CrosshairType.Star, CrosshairType.Brackets, CrosshairType.Arc
    ];

    [ObservableProperty] private int _pageIndex;
    [ObservableProperty] private Crosshair _demo = new()
    {
        Type = CrosshairType.Cross,
        Color = 0xFFFFFFFF,
        Size = 28,
        Thickness = 2,
        Gap = 4,
        ShowOutline = true,
        OutlineColor = 0xFF000000,
        ShowOuterRing = true,
        OuterRingSize = 42,
        OuterRingThickness = 1,
        OuterRingColor = 0x66FFFFFF
    };

    public OnboardingViewModel(Action completed)
    {
        Completed = completed;
        Loc.Current.LanguageChanged += Refresh;
    }

    public Loc Loc => Loc.Current;
    public Action Completed { get; }
    public string CurrentTitle => Loc[$"onboard{PageIndex}Title"];
    public string CurrentBody => Loc[$"onboard{PageIndex}Body"];
    public string CurrentCaption => Loc[$"onboard{PageIndex}Caption"];
    public bool IsLast => PageIndex >= 4;
    public string ActionTitle => IsLast ? Loc["getStarted"] : Loc["continue"];
    public bool ShowSkip => !IsLast;
    public string PageLabel => $"{PageIndex + 1} / 5";

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentTitle));
        OnPropertyChanged(nameof(CurrentBody));
        OnPropertyChanged(nameof(CurrentCaption));
        OnPropertyChanged(nameof(ActionTitle));
        OnPropertyChanged(nameof(PageLabel));
    }

    partial void OnPageIndexChanged(int value)
    {
        Refresh();
        OnPropertyChanged(nameof(IsLast));
        OnPropertyChanged(nameof(ShowSkip));
        Demo.Type = _tourTypes[Math.Clamp(value, 0, _tourTypes.Length - 1)];
        Demo.Size = 24 + value * 2;
        Demo.ShowOuterRing = value is 0 or 3;
        Demo.ShowTicks = value == 2;
        Demo.ShowCenterDot = value >= 2;
    }

    [RelayCommand]
    private void Next()
    {
        if (IsLast)
        {
            Finish();
            return;
        }

        PageIndex++;
    }

    [RelayCommand]
    private void Skip() => Finish();

    private void Finish() => Completed();
}
