using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CrosshairStudio.Domain;
using CrosshairStudio.ViewModels;

namespace CrosshairStudio.Overlay;

public partial class OverlayEditorWindow : Window
{
    public OverlayEditorWindow()
    {
        InitializeComponent();
    }

    public void Place(MonitorInfo monitor)
    {
        var width = (int)(Width * monitor.Scaling);
        var x = monitor.X + Math.Max(24, (monitor.Width - width) / 2);
        var y = monitor.Y + 48;
        Position = new PixelPoint(x, y);
        Topmost = true;
        NativeOverlay.Apply(this, clickThrough: false, alwaysOnTop: true, noActivate: false);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.HandleKey(e.Key, e.KeyModifiers))
        {
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Escape)
            return;
        if (DataContext is MainViewModel close)
            close.CloseOverlayEditor();
        e.Handled = true;
    }
}
