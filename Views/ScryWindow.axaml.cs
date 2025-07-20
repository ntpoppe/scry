using Avalonia.Controls;
using Scry.ViewModels;

namespace Scry.Views;

public partial class ScryWindow : Window
{
    public ScryWindow()
    {
        InitializeComponent();
        DataContext = new ScryWindowViewModel();
    }
}