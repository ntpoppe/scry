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
        var vm = new ScryWindowViewModel();
        DataContext = vm;

        vm.CaretMoveRequested += (_, _) => MoveCaretToEnd();
        vm.CancelRequested += (_, _) => Hide();
    }

    /// <summary> Used to ignore spaces if the command text is empty. Causes issues with parsing. </summary>
    private void CommandTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb
            && string.IsNullOrEmpty(tb.Text)
            && (e.Key == Key.Space || e.Key == Key.Tab))
        {
            e.Handled = true;
        }
    }
    public override void Show()
    {
        var vm = DataContext as ScryWindowViewModel;
        vm?.Reset();
        vm?.MoveDownCommand?.Execute(null);
        base.Show();
        CommandTextBox.Focus();
    }

    public override void Hide()
    {
        (DataContext as ScryWindowViewModel)?.Reset();
        base.Hide();
    }

    private void MoveCaretToEnd()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var len = CommandTextBox.Text?.Length ?? 0;
            CommandTextBox.CaretIndex = len;
            CommandTextBox.SelectionStart = len;
            CommandTextBox.SelectionEnd = len;
        }, DispatcherPriority.Background);
    }
}