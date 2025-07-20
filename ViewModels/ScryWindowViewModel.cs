using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scry.Services;
using System.Collections.Generic;

namespace Scry.ViewModels;

public partial class ScryWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _commandText = string.Empty;
    private readonly IProcessLauncher _launcher;

    public IRelayCommand LaunchCommand { get; set; }

    public ScryWindowViewModel() : this(new ProcessLauncher())
    {
    }

    public ScryWindowViewModel(IProcessLauncher launcher)
    {
        _launcher = launcher;
        LaunchCommand = new RelayCommand(OnLaunch);
    }

    private void OnLaunch()
    {
        _launcher.Launch(CommandText);
        // clear & hide window here via an event or callback
    }

    public List<string> Results { get; } = new List<string>()
    {
        "run notepad",
    };
}
