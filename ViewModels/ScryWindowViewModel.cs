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

    [ObservableProperty]
    private bool _executeReady;

    [ObservableProperty]
    private CommandPrefix? _currentPrefix;

    public IRelayCommand EnterCommand { get; }
    public IRelayCommand MoveDownCommand { get; }
    public IRelayCommand MoveUpCommand { get; }

    private readonly IProcessExecutor _executor;

    // Collection bound to ListBox, starts with prefixes then moves to executables
    public ObservableCollection<string> Items { get; } = new();

    // Map each prefix to its list of valid commands, temporary until fetch
    private readonly Dictionary<CommandPrefix, List<string>> _optionsMap =
        new()
        {
            { CommandPrefix.Run, new List<string> { "notepad", "calculator" } },
            { CommandPrefix.Web, new List<string> { "chatgpt", "youtube" } },
            { CommandPrefix.Script, new List<string>() }
        };

    public ScryWindowViewModel() : this(new ProcessExecutor()) { }

    public ScryWindowViewModel(IProcessExecutor executor)
    {
        _executor = executor;
        EnterCommand = new RelayCommand(EnterPressed);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        PopulateItems(GetPrefixes());
    }

    private void EnterPressed()
    {
        if (TryExecute()) return;
        if (SetPrefix()) return;
        ChooseCommand();
    }

    private bool TryExecute()
    {
        if (!ExecuteReady) return false;
        Execute();
        return true;
    }

    private bool SetPrefix()
    {
        if (CurrentPrefix == null && Enum.TryParse<CommandPrefix>(SelectedItem, true, out var prefix))
        {
            CurrentPrefix = prefix;
            CommandText = prefix.ToString().ToLowerInvariant();

            if (_optionsMap.TryGetValue(prefix, out var opts))
                PopulateItems(opts);

            MoveDown();
            return true;
        }

        return false;
    }

    private void ChooseCommand()
    {
        CommandText = $"{CurrentPrefix?.ToString().ToLowerInvariant()} {SelectedItem}";
        Items.Clear();
        ExecuteReady = true;
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
        ExecuteReady = false;
    }

    private void MoveUp()
    {
        if (SelectedIndex > 0)
            SelectedIndex--;
    }

    private void MoveDown()
    {
        if (SelectedIndex < Items.Count - 1)
            SelectedIndex++;
    }

    public IEnumerable<string> GetPrefixes()
        => Enum.GetNames<CommandPrefix>()
               .Select(name => name.ToLowerInvariant());
    public void PopulateItems(IEnumerable<string> values)
    {
        Items.Clear();
        foreach (var v in values) Items.Add(v);
    }

    public void Reset()
    {
        CommandText = string.Empty;
        ErrorMessage = null;
        ExecuteReady = false;
        CurrentPrefix = null;
        PopulateItems(GetPrefixes());
    }
}
