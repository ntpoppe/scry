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

    public IRelayCommand CancelCommand { get; }
    public IRelayCommand EnterCommand { get; }
    public IRelayCommand TabCommand { get; }
    public IRelayCommand MoveDownCommand { get; }
    public IRelayCommand MoveUpCommand { get; }

    public event EventHandler? CancelRequested;
    public event EventHandler? CaretMoveRequested;

    private readonly IProcessExecutor _executor;
    private bool _suppressChange;

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
        TabCommand = new RelayCommand(TabPressed);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        CancelCommand = new RelayCommand(Cancel);

        PopulateItems(GetPrefixes());
    }

    private void EnterPressed()
    {
        if (!ExecuteReady)
        {
            ErrorMessage = "Invalid command";
            return;
        }
        Execute();
    }

    private void TabPressed()
    {
        if (SetPrefix()) return;
        ChooseCommand();
    }

    private bool SetPrefix()
    {
        if (CurrentPrefix == null && Enum.TryParse<CommandPrefix>(SelectedItem, true, out var prefix))
        {
            CurrentPrefix = prefix;
            CommandText = prefix.ToString().ToLowerInvariant();
            CaretMoveRequested?.Invoke(this, EventArgs.Empty);

            if (_optionsMap.TryGetValue(prefix, out var opts))
                PopulateItems(opts);

            MoveDown();
            return true;
        }

        return false;
    }

    private void ChooseCommand()
    {
        if (ExecuteReady) return;

        CommandText = $"{CurrentPrefix?.ToString().ToLowerInvariant()} {SelectedItem}";
        CaretMoveRequested?.Invoke(this, EventArgs.Empty);
        Items.Clear();
        ExecuteReady = true;
    }

    partial void OnCommandTextChanged(string value)
    {
        if (_suppressChange) return;
        ErrorMessage = null;

        // If we had a prefix but the text no longer starts with it, reset
        if (CurrentPrefix is not null)
        {
            var expectedStart = CurrentPrefix.Value.ToString().ToLowerInvariant() + " ";
            if (!value.StartsWith(expectedStart, StringComparison.OrdinalIgnoreCase))
            {
                CurrentPrefix = null;
            }
        }

        var trimmed = value.TrimEnd();
        var parts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

        ExecuteReady = CheckIfExecutable(parts);

        // If we already have a prefix, filter commands  
        if (CurrentPrefix is not null)
        {
            var remainder = parts.Length > 1 ? parts[1] : string.Empty;
            var options = _optionsMap[CurrentPrefix.Value];

            PopulateItems(
                options.Where(o => o.StartsWith(remainder, StringComparison.OrdinalIgnoreCase))
            );
            MoveDown();
            return;
        }

        // No prefix yet -> have we typed one exactly?  
        if (parts.Length == 1)
        {
            var candidate = parts[0];
            // do we have a prefix whose name == candidate?  
            if (Enum.TryParse<CommandPrefix>(candidate, true, out var pfx))
            {
                // user has finished typing the prefix  
                CurrentPrefix = pfx;

                // include the trailing space in CommandText so they can start the next word  
                _suppressChange = true;
                CommandText = pfx.ToString().ToLowerInvariant() + " ";
                _suppressChange = false;

                CaretMoveRequested?.Invoke(this, EventArgs.Empty);

                // show that prefix's options  
                PopulateItems(_optionsMap[pfx]!);
                SelectedIndex = Items.Count > 0 ? 0 : -1;
                return;
            }
        }

        // Still typing a prefix -> live‐filter on what they've entered so far  
        var filter = parts.Length >= 1 ? parts[0] : string.Empty;
        PopulateItems(
            GetPrefixes()
              .Where(p => p.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
        );

        MoveDown();
    }

    private bool CheckIfExecutable(string[] parts)
    {
        if (parts.Length < 2 || CurrentPrefix is null)
            return false;

        if (_optionsMap.TryGetValue(CurrentPrefix.Value, out var opts))
            return opts.Any(o => o.Equals(parts[1].Trim(), StringComparison.OrdinalIgnoreCase));

        return false;
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
        Cancel();
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

    public void Cancel()
        => CancelRequested?.Invoke(this, EventArgs.Empty);

}
