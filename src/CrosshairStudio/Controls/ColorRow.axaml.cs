using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrosshairStudio.Domain;

namespace CrosshairStudio.Controls;

public partial class ColorRow : UserControl
{
    public ColorRow() => InitializeComponent();

    private void Preview_OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ColorSlot slot)
            slot.TogglePickerCommand.Execute(null);
        e.Handled = true;
    }

    private void Hex_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ColorSlot slot)
            return;
        if (sender is TextBox box)
            slot.Hex = box.Text ?? "";
        slot.CommitHex();
    }

    private void Hex_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        if (DataContext is ColorSlot slot && sender is TextBox box)
        {
            slot.Hex = box.Text ?? "";
            slot.CommitHex();
        }
        e.Handled = true;
    }
}
