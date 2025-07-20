using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scry.Models;
using Scry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Scry.ViewModels;

public partial class ScryWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _commandText = string.Empty;

    [ObservableProperty]
    private string? _selectedItem = null;

    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private string? _errorMessage;

    private readonly IProcessExecutor _executor;

    public IRelayCommand EnterCommand { get; }
    public IRelayCommand MoveDownCommand { get; }
    public IRelayCommand MoveUpCommand { get; }

    // Collection bound to ListBox, starts with prefixes then moves to executables
    public ObservableCollection<string> Items { get; } =
        new ObservableCollection<string>(Enum.GetNames<CommandPrefix>()
                                             .Select(n => n.ToLowerInvariant()));

    // Remember which prefix was chosen
    private CommandPrefix? _currentPrefix;

    // Map each prefix to its list of valid commands, temporary until fetch
    private readonly Dictionary<CommandPrefix, List<string>> _optionsMap =
        new()
        {
            { CommandPrefix.Run, new List<string> { "notepad", "calculator" } },
            { CommandPrefix.Web, new List<string> { "chatgpt", "youtube" } },
            { CommandPrefix.Exec, new List<string>() }
        };

    public ScryWindowViewModel() : this(new ProcessExecutor()) { }

    public ScryWindowViewModel(IProcessExecutor executor)
    {
        _executor = executor;
        EnterCommand = new RelayCommand(OnEnterPressed);
        MoveUpCommand = new RelayCommand(OnMoveUp);
        MoveDownCommand = new RelayCommand(OnMoveDown);
    }

    private void OnEnterPressed()
    {

    }

    private void Execute()
    {
        var result = _executor.Execute(CommandText);
        if (!result.Succeeded)
        {
            ErrorMessage = $"Could not execute: {result.ErrorMessage}";
            return;
        }

        CommandText = string.Empty;
        ErrorMessage = null;
    }

    private void OnMoveUp()
    {
        if (SelectedIndex > 0)
            SelectedIndex--;
    }

    private void OnMoveDown()
    {
        if (SelectedIndex < Items.Count - 1)
            SelectedIndex++;
    }

    partial void OnSelectedItemChanged(string? value)
    {
        // TODO: Find a better way to handle the else branch catching a null after click
        //if (value == null && _currentPrefix != null)
        //    return;

        //// If prefix entered, switch the items to the corresponding commands
        //if (Enum.TryParse<CommandPrefix>(value, true, out var prefix))
        //{
        //    _currentPrefix = prefix;
        //    CommandText = prefix.ToString().ToLowerInvariant();

        //    Items.Clear();
        //    if (_optionsMap.TryGetValue(prefix, out var opts))
        //        foreach (var opt in opts)
        //            Items.Add(opt);
        //}
        //else
        //{
        //    // Command clicked: inject it into the TextBox
        //    CommandText = $"{_currentPrefix?.ToString().ToLowerInvariant()} {value}";
        //    ResetItems();
        //}
    }

    public void ResetItems()
    {
        Items.Clear();
        foreach (var name in Enum.GetNames<CommandPrefix>())
            Items.Add(name.ToLowerInvariant());
    }
}
