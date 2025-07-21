using Avalonia.Controls;
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