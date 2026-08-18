using Avalonia;
using Avalonia.Controls;
using CrosshairStudio.Domain;

namespace CrosshairStudio.Infrastructure;

public static class MonitorQuery
{
    public static IReadOnlyList<MonitorInfo> Get(Visual? visual)
    {
        var screens = TopLevel.GetTopLevel(visual)?.Screens?.All
                      ?? (visual as Window)?.Screens?.All;
        if (screens == null || screens.Count == 0)
            return [new MonitorInfo { Name = "Display 1", Width = 1920, Height = 1080, Scaling = 1, IsPrimary = true }];

        var list = new List<MonitorInfo>(screens.Count);
        for (var i = 0; i < screens.Count; i++)
        {
            var screen = screens[i];
            list.Add(new MonitorInfo
            {
                Index = i,
                Name = $"Display {i + 1}",
                X = screen.Bounds.X,
                Y = screen.Bounds.Y,
                Width = screen.Bounds.Width,
                Height = screen.Bounds.Height,
                Scaling = screen.Scaling <= 0 ? 1 : screen.Scaling,
                IsPrimary = screen.IsPrimary
            });
        }

        return list;
    }
}
