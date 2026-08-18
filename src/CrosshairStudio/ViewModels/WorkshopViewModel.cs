using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosshairStudio.Domain;
using CrosshairStudio.Infrastructure;
using CrosshairStudio.Infrastructure.Localization;

namespace CrosshairStudio.ViewModels;

public partial class WorkshopViewModel : ViewModelBase
{
    private readonly WorkshopClient _client;
    private readonly MainViewModel _main;
    private CancellationTokenSource? _load;

    [ObservableProperty] private string _kind = "crosshair";
    [ObservableProperty] private string _sort = "likes";
    [ObservableProperty] private string _query = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "";

    public WorkshopViewModel(WorkshopClient client, MainViewModel main)
    {
        _client = client;
        _main = main;
    }

    public Loc Loc => Loc.Current;
    public ObservableCollection<WorkshopCard> Items { get; } = [];
    public bool IsCrosshairs => Kind == "crosshair";
    public bool IsWidgets => Kind == "widget";

    partial void OnKindChanged(string value)
    {
        OnPropertyChanged(nameof(IsCrosshairs));
        OnPropertyChanged(nameof(IsWidgets));
        _ = LoadAsync();
    }

    partial void OnSortChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private Task Search() => LoadAsync();

    [RelayCommand]
    private void ShowCrosshairs() => Kind = "crosshair";

    [RelayCommand]
    private void ShowWidgets() => Kind = "widget";

    [RelayCommand]
    private void SortLikes() => Sort = "likes";

    [RelayCommand]
    private void SortNew() => Sort = "new";

    public async Task LoadAsync()
    {
        _load?.Cancel();
        _load = new CancellationTokenSource();
        var ct = _load.Token;
        IsBusy = true;
        Status = Loc["workshopLoading"];
        try
        {
            var items = await _client.ListAsync(Kind, Sort, Query, ct);
            Items.Clear();
            foreach (var item in items)
                Items.Add(WorkshopCard.From(item));
            Status = items.Count == 0 ? Loc["workshopEmpty"] : string.Format(Loc["workshopCount"], items.Count);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            Status = Loc["workshopOffline"];
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Like(WorkshopCard? card)
    {
        if (card == null)
            return;
        try
        {
            var updated = await _client.ToggleLikeAsync(card.Id);
            if (updated == null)
                return;
            card.Liked = updated.Liked;
            card.LikeCount = updated.LikeCount;
        }
        catch
        {
            Status = Loc["workshopOffline"];
        }
    }

    [RelayCommand]
    private void Add(WorkshopCard? card)
    {
        if (card == null)
            return;
        try
        {
            if (card.Kind == "widget")
            {
                var widget = card.Payload.Deserialize<HudWidget>(WorkshopClient.Json);
                if (widget == null)
                    return;
                _main.AddWorkshopWidget(widget, card.Id);
            }
            else
            {
                var design = card.Payload.Deserialize<Crosshair>(WorkshopClient.Json);
                if (design == null)
                    return;
                _main.AddWorkshopDesign(design, card.Id);
            }
            Status = Loc["workshopAdded"];
        }
        catch
        {
            Status = Loc["workshopAddFail"];
        }
    }
}

public partial class WorkshopCard : ObservableObject
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Author { get; init; } = "";
    public JsonElement Payload { get; init; }
    public Crosshair? Preview { get; init; }
    public bool IsCrosshair => Kind == "crosshair";

    [ObservableProperty] private int _likeCount;
    [ObservableProperty] private bool _liked;

    public static WorkshopCard From(WorkshopItem item)
    {
        Crosshair? preview = null;
        if (item.Kind == "crosshair")
        {
            try
            {
                preview = item.Payload.Deserialize<Crosshair>(WorkshopClient.Json);
                if (preview != null)
                    preview.ExtraParts ??= [];
            }
            catch
            {
                preview = null;
            }
        }

        return new WorkshopCard
        {
            Id = item.Id,
            Kind = item.Kind,
            Name = item.Name,
            Description = item.Description,
            Author = item.Author,
            Payload = item.Payload,
            Preview = preview,
            LikeCount = item.LikeCount,
            Liked = item.Liked
        };
    }
}
