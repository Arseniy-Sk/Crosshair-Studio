using CrosshairStudio.Domain;
using CrosshairStudio.Infrastructure.Sampling;

namespace CrosshairStudio.Overlay;

internal static class WidgetTemplate
{
    public static bool UsesSystem(string? text)
        => ContainsAny(text, "cpu", "ram", "system");

    public static bool UsesPing(string? text)
        => ContainsAny(text, "ping");

    public static string Render(HudWidget widget, TelemetrySampler telemetry, HudStats stats, TimeSpan session)
    {
        var source = widget.Text;
        if (string.IsNullOrWhiteSpace(source))
            return string.IsNullOrWhiteSpace(widget.Name) ? " " : widget.Name;

        var now = DateTime.Now;
        var output = new System.Text.StringBuilder(source.Length + 16);
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == '{' && i + 1 < source.Length)
            {
                var close = source.IndexOf('}', i + 1);
                if (close > i)
                {
                    var token = source[(i + 1)..close].Trim();
                    output.Append(Resolve(token, widget, telemetry, stats, session, now));
                    i = close;
                    continue;
                }
            }

            output.Append(source[i]);
        }

        var text = output.ToString();
        return string.IsNullOrWhiteSpace(text) ? " " : text;
    }

    private static string Resolve(string token, HudWidget widget, TelemetrySampler telemetry, HudStats stats, TimeSpan session, DateTime now)
    {
        return token.ToLowerInvariant() switch
        {
            "time" => now.ToString("HH:mm:ss"),
            "hh" => now.ToString("HH"),
            "mm" => now.ToString("mm"),
            "ss" => now.ToString("ss"),
            "date" => stats.FormatDate(),
            "session" => FormatSpan(session),
            "cpu" => $"{telemetry.StudioCpu:0.0}%",
            "ram" => $"{telemetry.RamUsedGb:0.0}/{telemetry.RamTotalGb:0.0} GB",
            "system" => telemetry.FormatSystem(),
            "ping" => telemetry.FormatPing(),
            "battery" => stats.FormatBattery(),
            "uptime" => stats.FormatUptime(),
            "count" or "n" => widget.Count.ToString(),
            "left" => widget.ScoreLeft.ToString(),
            "right" => widget.ScoreRight.ToString(),
            "name" => string.IsNullOrWhiteSpace(widget.Name) ? "" : widget.Name,
            _ => "{" + token + "}"
        };
    }

    private static bool ContainsAny(string? text, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        foreach (var token in tokens)
        {
            if (text.Contains("{" + token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string FormatSpan(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        return span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss") : span.ToString(@"mm\:ss");
    }
}
