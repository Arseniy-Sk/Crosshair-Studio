using Avalonia;
using System;
using System.IO;

namespace CrosshairStudio;

sealed class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception ex)
        {
            TryLog(ex);
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void TryLog(Exception ex)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairStudio");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"[{DateTime.Now:u}] {ex}\n\n");
        }
        catch
        {
            // Ignore logging failures.
        }
    }
}
