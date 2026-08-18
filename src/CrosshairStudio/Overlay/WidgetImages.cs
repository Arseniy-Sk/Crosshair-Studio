using Avalonia.Media.Imaging;

namespace CrosshairStudio.Overlay;

internal static class WidgetImages
{
    private static readonly Dictionary<string, Bitmap> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Bitmap? Get(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        if (Cache.TryGetValue(path, out var cached))
            return cached;

        try
        {
            var bitmap = new Bitmap(path);
            Cache[path] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
