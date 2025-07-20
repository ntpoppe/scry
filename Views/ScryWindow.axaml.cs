using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Scry.ViewModels;

namespace Scry.Views;

public partial class ScryWindow : Window
{
    public ScryWindow()
    {
        InitializeComponent();
        DataContext = new ScryWindowViewModel();

        CommandTextBox.TextChanged += CommandTextBox_TextChanged;
    }

    private void CommandTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        // Post to UI thread after layout so the new text is already in place
        Dispatcher.UIThread.Post(() =>
        {
            var len = CommandTextBox.Text?.Length ?? 0;
            CommandTextBox.CaretIndex = len;
            CommandTextBox.SelectionStart = len;
            CommandTextBox.SelectionEnd = len;
        }, DispatcherPriority.Background);
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
        (DataContext as ScryWindowViewModel)?.Reset();
        base.Show();
        CommandTextBox.Focus();
    }

    public override void Hide()
    {
        (DataContext as ScryWindowViewModel)?.Reset();
        base.Hide();
    }
}