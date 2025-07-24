using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Scry.ViewModels;

public partial class ScryWindowViewModel : ViewModelBase
{
    private readonly ProcessExecutor _executor;
    private bool _suppressChange;

    public event EventHandler? CancelRequested;
    public event EventHandler? CaretMoveRequested;

    [ObservableProperty]
    private string _commandText = string.Empty;

    [ObservableProperty]
    private string? _selectedItem = null;

    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _executeReadyByExactMatch;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivePhase))]
    private ICommandHandler? _currentHandler;

    public string ActivePhase => CurrentHandler == null ? "action" : "argument";

    public IRelayCommand CancelCommand { get; }
    public IRelayCommand EnterCommand { get; }
    public IRelayCommand TabCommand { get; }
    public IRelayCommand MoveDownCommand { get; }
    public IRelayCommand MoveUpCommand { get; }

    public ObservableCollection<string> Items { get; } = new();

    public ScryWindowViewModel() : this(new ProcessExecutor()) { }

    public ScryWindowViewModel(ProcessExecutor executor)
    {
        _executor = executor;

        EnterCommand = new RelayCommand(EnterPressed);
        TabCommand = new RelayCommand(TabPressed);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        CancelCommand = new RelayCommand(Cancel);

        PopulateItems(GetPrefixes());
    }

    partial void OnCommandTextChanged(string value)
    {
        if (_suppressChange) return;
        ErrorMessage = null;

        // if we had a prefix but the text no longer starts with it, reset
        if (CurrentHandler is not null)
        {
            var expectedStart = CurrentHandler.Prefix + " ";
            if (!value.StartsWith(expectedStart, StringComparison.OrdinalIgnoreCase))
            {
                CurrentHandler = null;
            }
        }

        var parts = value.Split(new[] { ' ' }, 2, StringSplitOptions.None);

        ExecuteReadyByExactMatch = CheckIfExecutable(parts);

        // if we already have a prefix, filter commands  
        if (CurrentHandler is not null)
        {
            var remainder = parts.Length > 1 ? parts[1] : string.Empty;
            var prefixKey = CurrentHandler.Prefix;
            var commands = _executor.GetOptions(prefixKey);

            PopulateItems(
                commands
                    .Where(cmd => cmd.StartsWith(remainder, StringComparison.OrdinalIgnoreCase))
            );

            MoveDown();
            return;
        }

        // no prefix yet -> have we typed one exactly?  
        if (parts.Length == 1)
        {
            var candidate = parts[0];
            // do we have a prefix whose name == candidate?  
            if (_executor.TryGetHandler(candidate, out var handler))
            {
                // user has finished typing the prefix  
                CurrentHandler = handler;

                // include the trailing space in CommandText so they can start the next word  
                _suppressChange = true;
                CommandText = CurrentHandler.Prefix.ToLowerInvariant() + " ";
                _suppressChange = false;

                CaretMoveRequested?.Invoke(this, EventArgs.Empty);

                // show that prefix's options  
                var prefixKey = CurrentHandler.Prefix;
                var options = _executor.GetOptions(prefixKey);
                PopulateItems(options);

                MoveDown();
                return;
            }
        }

        // still typing a prefix -> live‐filter on what they've entered so far  
        var filter = parts.Length >= 1 ? parts[0] : string.Empty;
        PopulateItems(
            GetPrefixes()
              .Where(p => p.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
        );

        MoveDown();
    }

    private void EnterPressed()
    {
        // fully-typed commands
        if (ExecuteReadyByExactMatch)
        {
            Execute();
            return;
        }

        // no handler yet -> pick the handler name
        if (CurrentHandler == null && SelectedItem != null)
        {
            CommandText = $"{SelectedItem}";
            CaretMoveRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        // have a handler + a selection -> build “prefix argument” and run
        if (CurrentHandler != null && SelectedItem != null)
        {
            CommandText = $"{CurrentHandler.Prefix} {SelectedItem}";
            Execute();
            return;
        }

        ErrorMessage = "Invalid command";
    }

    private void TabPressed()
    {
        if (SetPrefix()) return;
        ChooseCommand();
    }

    private bool SetPrefix()
    {
        // if we don’t yet have a handler selected, and the SelectedItem is one of our prefixes…
        if (CurrentHandler == null
            && SelectedItem is not null
            && _executor.TryGetHandler(SelectedItem.ToLowerInvariant(), out var handler))
        {
            CurrentHandler = handler;

            _suppressChange = true;
            CommandText = handler.Prefix + " ";
            _suppressChange = false;

            CaretMoveRequested?.Invoke(this, EventArgs.Empty);

            // load that handler’s options
            PopulateItems(handler.GetOptions());
            SelectedIndex = Items.Any() ? 0 : -1;

            return true;
        }

        return false;
    }

    private void ChooseCommand()
    {
        if (ExecuteReadyByExactMatch) return;

        CommandText = $"{CurrentHandler?.Prefix} {SelectedItem}";
        CaretMoveRequested?.Invoke(this, EventArgs.Empty);
        Items.Clear();
        ExecuteReadyByExactMatch = true;
    }

    private bool CheckIfExecutable(string[] parts)
    {
        if (parts.Length < 2 || CurrentHandler is null)
            return false;

        var prefixKey = CurrentHandler.Prefix;
        var opts = _executor.GetOptions(prefixKey);
        return opts.Any(o => o.Equals(parts[1].Trim(), StringComparison.OrdinalIgnoreCase));
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
        ExecuteReadyByExactMatch = false;
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
        => _executor.ValidPrefixes;

    public void PopulateItems(IEnumerable<string> values)
    {
        Items.Clear();
        foreach (var v in values) Items.Add(v);
    }
    public void Reset()
    {
        CommandText = string.Empty;
        ErrorMessage = null;
        ExecuteReadyByExactMatch = false;
        CurrentHandler = null;
        PopulateItems(GetPrefixes());
    }

    public void Cancel()
        => CancelRequested?.Invoke(this, EventArgs.Empty);
}
