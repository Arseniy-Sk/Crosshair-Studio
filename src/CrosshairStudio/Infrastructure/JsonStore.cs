using System.Text.Json;
using CrosshairStudio.Domain;
using CrosshairStudio.Infrastructure.Localization;

namespace CrosshairStudio.Infrastructure;

public sealed class JsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _root;
    private readonly string _profilesDir;
    private readonly string _settingsPath;

    public JsonStore()
    {
        _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairStudio");
        _profilesDir = Path.Combine(_root, "Profiles");
        _settingsPath = Path.Combine(_root, "settings.json");
        Directory.CreateDirectory(_profilesDir);
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath), Options) ?? new AppSettings();
                loaded.Display ??= new DisplaySettings();
                loaded.Widgets ??= [];
                loaded.Library ??= [];
                loaded.OverlayHotkey ??= HotkeyBinding.Default();
                if (loaded.OverlayHotkey.VirtualKey == 0)
                    loaded.OverlayHotkey = HotkeyBinding.Default();
                loaded.Crosshair ??= null;
                if (loaded.Crosshair != null)
                    loaded.Crosshair.ExtraParts ??= [];
                foreach (var item in loaded.Library)
                    item.ExtraParts ??= [];
                foreach (var widget in loaded.Widgets)
                {
                    if (widget.Scale <= 0)
                        widget.Scale = 1;
                    if (widget.Zoom <= 0)
                        widget.Zoom = 3;
                    if (widget.CaptureSize <= 0)
                        widget.CaptureSize = 88;
                }
                if (string.IsNullOrWhiteSpace(loaded.ClientId))
                    loaded.ClientId = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(loaded.WorkshopUrl))
                    loaded.WorkshopUrl = "http://150.251.152.203:8787";
                if (string.IsNullOrWhiteSpace(loaded.DisplayName))
                    loaded.DisplayName = "Player";
                if (string.IsNullOrWhiteSpace(loaded.Language))
                    loaded.Language = Loc.GuessLanguage();
                return loaded;
            }
        }
        catch
        {
            // Keep defaults if the file is from an older version.
        }

        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, Options));
    }

    public IReadOnlyList<Crosshair> LoadProfiles()
    {
        var list = new List<Crosshair>();
        foreach (var file in Directory.GetFiles(_profilesDir, "*.json"))
        {
            try
            {
                var profile = JsonSerializer.Deserialize<Crosshair>(File.ReadAllText(file), Options);
                if (profile != null)
                {
                    profile.ExtraParts ??= [];
                    list.Add(profile);
                }
            }
            catch
            {
                // Skip unreadable profiles.
            }
        }

        return list;
    }

    public void SaveProfile(Crosshair crosshair)
    {
        var path = Path.Combine(_profilesDir, Sanitize(crosshair.Name) + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(crosshair, Options));
    }

    public void DeleteProfile(Crosshair crosshair)
    {
        var path = Path.Combine(_profilesDir, Sanitize(crosshair.Name) + ".json");
        if (File.Exists(path))
            File.Delete(path);
    }

    public void Export(Crosshair crosshair, string path)
        => File.WriteAllText(path, JsonSerializer.Serialize(crosshair, Options));

    public Crosshair? Import(string path)
        => JsonSerializer.Deserialize<Crosshair>(File.ReadAllText(path), Options);

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
