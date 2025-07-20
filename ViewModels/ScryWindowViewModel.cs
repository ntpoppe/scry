using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scry.Services;
using System.Collections.Generic;

namespace Scry.ViewModels;

public partial class ScryWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _commandText = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    private readonly IProcessLauncher _launcher;

    public IRelayCommand LaunchCommand { get; }

    public ScryWindowViewModel()
    {
        LaunchCommand = new RelayCommand(OnLaunch);
        _launcher = new ProcessLauncher();
    }

    private void OnLaunch()
    {
        var result = _launcher.Launch(CommandText);
        if (!result.Succeeded)
        {
            ErrorMessage = $"Could not launch: {result.ErrorMessage}";
            return;
        }

        CommandText = string.Empty;
        ErrorMessage = null;
    }

    public List<string> Results { get; } = new List<string>()
    {
        "run notepad",
    };
}
