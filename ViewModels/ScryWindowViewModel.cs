using System.Collections.Generic;

namespace Scry.ViewModels;

public partial class ScryWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";
    public string Query { get; set; } = string.Empty;
    public string SelectedResult { get; set; } = string.Empty;
    public List<string> Results { get; } = new List<string>()
    {
        "Test 1",
        "Test 2"
    };
}
