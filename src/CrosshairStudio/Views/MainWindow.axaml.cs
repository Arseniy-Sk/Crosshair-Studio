using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CrosshairStudio.Infrastructure;
using CrosshairStudio.ViewModels;

namespace CrosshairStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            ApplyChrome();
            if (DataContext is MainViewModel vm)
                vm.AttachHost(this);
        };
        Resized += (_, _) => ApplyChrome();
        PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(WindowState))
            {
                Shell.CornerRadius = WindowState == WindowState.Maximized ? new CornerRadius(0) : new CornerRadius(18);
                ApplyChrome();
            }
        };
    }

    private void ApplyChrome() => WindowChrome.Apply(this);

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (IsChromeButton(e.Source))
            return;

        if (e.ClickCount >= 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
            return;
        }

        BeginMoveDrag(e);
    }

    private static bool IsChromeButton(object? source)
    {
        var current = source as Control;
        while (current != null)
        {
            if (current is Button)
                return true;
            current = current.Parent as Control;
        }

        return false;
    }

    private void Minimize_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.HandleKey(e.Key, e.KeyModifiers))
            e.Handled = true;
    }
}
