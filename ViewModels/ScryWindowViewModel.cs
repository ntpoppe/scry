using Avalonia.Input.Platform;
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
    private readonly ProcessExecutor _executor;
    private bool _suppressChange;

    public event EventHandler? CancelRequested;
    public event EventHandler? CaretMoveRequested;

    [ObservableProperty]
    private string _commandText = string.Empty;

    [ObservableProperty]
    private ListEntry? _selectedItem = null;

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

    public IRelayCommand ItemClickCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand EnterCommand { get; }
    public IRelayCommand TabCommand { get; }
    public IRelayCommand MoveDownCommand { get; }
    public IRelayCommand MoveUpCommand { get; }

    public ObservableCollection<ListEntry> Items { get; } = new();

#pragma warning disable CS8618
    public ScryWindowViewModel() { }
#pragma warning restore CS8618

    public ScryWindowViewModel(IClipboard clipboard) : this(new ProcessExecutor(clipboard)) { }

    public ScryWindowViewModel(ProcessExecutor executor)
    {
        _executor = executor;

        EnterCommand = new RelayCommand(EnterPressed);
        TabCommand = new RelayCommand(TabPressed);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        CancelCommand = new RelayCommand(Cancel);
        ItemClickCommand = new RelayCommand<ListEntry>(ItemClick);

        PopulateItems(GetListEntries());
    }

    partial void OnCommandTextChanged(string value)
    {
        if (_suppressChange) return;
        ErrorMessage = null;

        // Reset handler if command text no longer starts with current prefix
        ResetHandlerIfNeeded(value);

        var parts = value.Split(new[] { ' ' }, 2, StringSplitOptions.None);
        ExecuteReadyByExactMatch = CheckIfExecutable(parts);

        if (CurrentHandler != null)
            HandleArgumentFiltering(parts);
        else
            HandlePrefixFiltering(parts);
    }

    private void EnterPressed()
    {
        // Execute if we have a complete command
        if (ExecuteReadyByExactMatch)
        {
            Execute();
            return;
        }

        if (CurrentHandler?.IsEntryless == true && !string.IsNullOrWhiteSpace(CommandText.Trim()))
        {
            Execute();
            return;
        }

        if (SelectedItem == null)
        {
            ErrorMessage = "Invalid command";
            return;
        }

        // Handle selection-based completion
        if (CurrentHandler == null)
            CompletePrefixSelection(SelectedItem);
        else
            CompleteAndExecuteArgument(SelectedItem);
    }

    private void TabPressed()
    {
        if (SetPrefix()) return;

        if (CurrentHandler != null && SelectedItem != null)
            CompleteArgument(SelectedItem);
    }

    /// <summary>
    /// Resets the current handler if the command text no longer starts with the expected prefix
    /// </summary>
    private void ResetHandlerIfNeeded(string commandText)
    {
        if (CurrentHandler == null) return;

        var expectedStart = CurrentHandler.Prefix + " ";
        if (!commandText.StartsWith(expectedStart, StringComparison.OrdinalIgnoreCase))
            CurrentHandler = null;
    }

    /// <summary>
    /// Handles filtering when we have a prefix selected and are typing arguments
    /// </summary>
    private void HandleArgumentFiltering(string[] parts)
    {
        var remainder = parts.Length > 1 ? parts[1] : string.Empty;

        if (CurrentHandler!.IsEntryless)
        {
            PopulateItems(Enumerable.Empty<ListEntry>());
            return;
        }

        var commands = _executor.GetOptions(CurrentHandler.Prefix);
        PopulateItems(
            commands.Where(cmd => IsFuzzyMatch(cmd.Value, remainder))
        );

        MoveDown();
    }

    /// <summary>
    /// Handles filtering when we're still typing the prefix
    /// </summary>
    private void HandlePrefixFiltering(string[] parts)
    {
        // Check if we've typed a complete prefix
        if (parts.Length == 1 && TrySetPrefixFromText(parts[0]))
            return;

        // Still typing prefix - filter available prefixes
        var filter = parts.Length >= 1 ? parts[0] : string.Empty;
        PopulateItems(
            GetListEntries().Where(p => IsFuzzyMatch(p.Value, filter))
        );

        MoveDown();
    }

    /// <summary>
    /// Attempts to set the current handler based on the typed text
    /// </summary>
    private bool TrySetPrefixFromText(string candidate)
    {
        if (_executor.TryGetHandler(candidate, out var handler))
        {
            SetCurrentHandler(handler);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the current handler and updates the UI accordingly
    /// </summary>
    private void SetCurrentHandler(ICommandHandler handler)
    {
        CurrentHandler = handler;

        _suppressChange = true;
        CommandText = handler.Prefix.ToLowerInvariant() + " ";
        _suppressChange = false;

        CaretMoveRequested?.Invoke(this, EventArgs.Empty);

        // Load options for this handler
        var options = _executor.GetOptions(handler.Prefix);
        PopulateItems(options);

        MoveDown();
    }

    /// <summary>
    /// Completes a prefix selection (Tab behavior)
    /// </summary>
    private void CompletePrefixSelection(ListEntry entry)
    {
        if (_executor.TryGetHandler(entry.Value.ToLowerInvariant(), out var handler))
        {
            SetCurrentHandler(handler);
        }
        else
        {
            // If it's not a prefix, treat as direct command? Not sure
            CommandText = entry.Value;
            Execute();
        }
    }

    /// <summary>
    /// Completes an argument and executes it (Enter behavior)
    /// </summary>
    private void CompleteAndExecuteArgument(ListEntry entry)
    {
        var completeCommand = $"{CurrentHandler!.Prefix} {entry.Value}";
        CommandText = completeCommand;
        Execute();
    }

    /// <summary>
    /// Completes a command without executing (Tab behavior)
    /// </summary>
    private void CompleteArgument(ListEntry entry)
    {
        CommandText = $"{CurrentHandler!.Prefix} {entry.Value}";
        CaretMoveRequested?.Invoke(this, EventArgs.Empty);
        Items.Clear();
        ExecuteReadyByExactMatch = true;
    }

    private bool SetPrefix()
    {
        if (CurrentHandler == null && SelectedItem != null)
            return TrySetPrefixFromText(SelectedItem.Value.ToLowerInvariant());

        return false;
    }

    private bool CheckIfExecutable(string[] parts)
    {
        if (parts.Length < 2 || CurrentHandler is null)
            return false;

        var argument = string.Join(" ", parts.Skip(1)).Trim();

        // Entryless handlers can execute with any non-empty argument
        if (CurrentHandler.IsEntryless)
            return !string.IsNullOrWhiteSpace(argument);

        // For handlers with predefined options, check if argument matches
        var prefixKey = CurrentHandler.Prefix;
        var opts = _executor.GetOptions(prefixKey);
        return opts.Any(o => o.Value.Equals(argument, StringComparison.OrdinalIgnoreCase));
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

    private void ItemClick(ListEntry? entry)
    {
        if (entry == null) return;

        // Case 1: No current handler - this is a prefix selection
        if (CurrentHandler == null)
        {
            HandlePrefixSelection(entry);
            return;
        }

        // Case 2: We have a handler and this is an argument selection
        if (CurrentHandler != null)
        {
            HandleArgumentSelection(entry);
            return;
        }
    }

    /// <summary>
    /// Handles selection of a command prefix (e.g., "run", "web", etc.)
    /// Sets the prefix and loads available options for that command type
    /// </summary>
    private void HandlePrefixSelection(ListEntry entry)
    {
        // Check if this entry represents a valid command prefix
        if (_executor.TryGetHandler(entry.Value.ToLowerInvariant(), out var handler))
        {
            // Set the current handler and update command text
            CurrentHandler = handler;

            _suppressChange = true;
            CommandText = $"{handler.Prefix} ";
            _suppressChange = false;

            // Move caret to end of command text
            CaretMoveRequested?.Invoke(this, EventArgs.Empty);

            // Load available options for this command type
            PopulateItems(handler.GetOptions());
            SelectedIndex = Items.Any() ? 0 : -1;
        }
        else
        {
            // If it's not a prefix, treat it as a direct command execution
            CommandText = entry.Value;
            Execute();
        }
    }

    /// <summary>
    /// Handles selection of an argument for the current command
    /// Either completes the command text or executes it directly
    /// </summary>
    private void HandleArgumentSelection(ListEntry entry)
    {
        // Build the complete command with prefix and selected argument
        var completeCommand = $"{CurrentHandler!.Prefix} {entry.Value}";

        // Check if this is a valid executable command
        if (CheckIfExecutable(completeCommand.Split(' ')))
        {
            // Valid command - execute it directly
            CommandText = completeCommand;
            Execute();
        }
        else
        {
            // Not a complete command yet - just update the text
            CommandText = completeCommand;
            CaretMoveRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    // null check added to satisfy designer...
    public IEnumerable<ListEntry> GetListEntries()
    {
        if (_executor == null)
            return new[] { new ListEntry("run", "launch apps") };

        return _executor.ListEntries;
    }

    public void PopulateItems(IEnumerable<ListEntry> values)
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
        PopulateItems(GetListEntries());
    }

    public void Cancel()
        => CancelRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Performs fuzzy matching between a target string and a query
    /// Returns true if the query characters appear in the target in order (case-insensitive)
    /// </summary>
    private bool IsFuzzyMatch(string target, string query)
    {
        if (string.IsNullOrEmpty(query)) return true;
        if (string.IsNullOrEmpty(target)) return false;

        // First try exact prefix match (highest priority)
        if (target.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return true;

        // Then try fuzzy match
        var targetLower = target.ToLowerInvariant();
        var queryLower = query.ToLowerInvariant();

        int queryIndex = 0;
        int targetIndex = 0;

        while (queryIndex < queryLower.Length && targetIndex < targetLower.Length)
        {
            if (queryLower[queryIndex] == targetLower[targetIndex])
                queryIndex++;

            targetIndex++;
        }

        return queryIndex == queryLower.Length;
    }
}
