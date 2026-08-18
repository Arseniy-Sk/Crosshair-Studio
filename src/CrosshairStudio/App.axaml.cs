using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CrosshairStudio.Infrastructure;
using CrosshairStudio.Overlay;
using CrosshairStudio.ViewModels;
using CrosshairStudio.Views;

namespace CrosshairStudio;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                ProgramLog(e.Exception);
                e.Handled = true;
            };

            var store = new JsonStore();
            var settings = store.LoadSettings();
            var overlay = new OverlayService();
            var workshop = new WorkshopClient(settings);
            var main = new MainViewModel(store, overlay, settings, workshop);

            desktop.ShutdownRequested += (_, _) =>
            {
                main.Shutdown();
                overlay.Dispose();
            };

            desktop.MainWindow = new MainWindow { DataContext = main };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ProgramLog(Exception ex)
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
