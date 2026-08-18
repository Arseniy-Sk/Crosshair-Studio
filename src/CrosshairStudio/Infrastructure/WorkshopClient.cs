using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrosshairStudio.Domain;

namespace CrosshairStudio.Infrastructure;

public sealed class WorkshopClient
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AppSettings _settings;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public WorkshopClient(AppSettings settings) => _settings = settings;

    public string BaseUrl
    {
        get => string.IsNullOrWhiteSpace(_settings.WorkshopUrl)
            ? "http://150.251.152.203:8787"
            : _settings.WorkshopUrl.Trim().TrimEnd('/');
        set => _settings.WorkshopUrl = value.Trim().TrimEnd('/');
    }

    public async Task<IReadOnlyList<WorkshopItem>> ListAsync(string kind, string sort, string query, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/api/workshop?kind={Uri.EscapeDataString(kind)}&sort={Uri.EscapeDataString(sort)}&q={Uri.EscapeDataString(query ?? "")}";
        using var req = WithClient(new HttpRequestMessage(HttpMethod.Get, url));
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<ListResponse>(Json, ct);
        return payload?.Items ?? [];
    }

    public async Task<WorkshopItem?> PublishAsync(string kind, string? id, string name, string description, bool listed, object payload, CancellationToken ct = default)
    {
        var body = new PublishRequest
        {
            Id = id,
            Kind = kind,
            Name = name,
            Description = description,
            Listed = listed,
            Author = string.IsNullOrWhiteSpace(_settings.DisplayName) ? "Player" : _settings.DisplayName.Trim(),
            Payload = payload
        };
        using var req = WithClient(new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/workshop")
        {
            Content = JsonContent.Create(body, options: Json)
        });
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WorkshopItem>(Json, ct);
    }

    public async Task<WorkshopItem?> SetListedAsync(string id, bool listed, CancellationToken ct = default)
    {
        using var req = WithClient(new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/api/workshop/{Uri.EscapeDataString(id)}")
        {
            Content = JsonContent.Create(new { listed }, options: Json)
        });
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WorkshopItem>(Json, ct);
    }

    public async Task<WorkshopItem?> ToggleLikeAsync(string id, CancellationToken ct = default)
    {
        using var req = WithClient(new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/workshop/{Uri.EscapeDataString(id)}/like"));
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WorkshopItem>(Json, ct);
    }

    private HttpRequestMessage WithClient(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("X-Client-Id", _settings.ClientId);
        return request;
    }

    private sealed class ListResponse
    {
        public List<WorkshopItem> Items { get; set; } = [];
    }

    private sealed class PublishRequest
    {
        public string? Id { get; set; }
        public string Kind { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Listed { get; set; }
        public string Author { get; set; } = "";
        public object? Payload { get; set; }
    }
}

public sealed class WorkshopItem
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public bool Listed { get; set; }
    public int LikeCount { get; set; }
    public bool Liked { get; set; }
    public bool Owned { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public JsonElement Payload { get; set; }
}
