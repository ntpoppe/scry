using Avalonia.Controls;
using Avalonia.Input;
using Scry.ViewModels;

namespace Scry.Views;

public partial class ScryWindow : Window
{
    public ScryWindow()
    {
        InitializeComponent();
        DataContext = new ScryWindowViewModel();

        CommandTextBox.LostFocus += CommandTextBox_LostFocus;
    }

    private void CommandTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ScryWindowViewModel vm)
            vm.CommandText = string.Empty;

        CommandTextBox.Clear();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            base.OnKeyDown(e);
            return;
        }

        Hide();
        e.Handled = true;
    }

    public override void Show()
    {
        if (DataContext is ScryWindowViewModel vm)
            vm.CommandText = string.Empty;

        CommandTextBox.Clear();

        base.Show();

        CommandTextBox.Focus();
    }
}