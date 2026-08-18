using Avalonia.Controls;
using Avalonia.Input;
using CrosshairStudio.ViewModels;

namespace CrosshairStudio.Views;

public partial class WorkshopPage : UserControl
{
    public WorkshopPage() => InitializeComponent();

    private void Search_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        if (DataContext is MainViewModel vm)
            vm.Workshop.SearchCommand.Execute(null);
        e.Handled = true;
    }
}
