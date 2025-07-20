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
}