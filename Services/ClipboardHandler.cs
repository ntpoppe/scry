using Avalonia.Input.Platform;
using Scry.Models;
using Scry.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Scry.Handlers;

public class ClipboardHandler : ICommandHandler
{
    private readonly ObservableCollection<ListEntry> _historyEntries = new();

    public string Prefix => "clipboard";
    public string Description => "clipboard history";
    public bool IsEntryless => false;

    private readonly List<string> _historyTexts = new();
    private readonly IClipboard _clipboard;
    private string? _lastSeen;

    public ClipboardHandler(IClipboard clipboard)
    {
        _clipboard = clipboard;
        _ = StartPolling();
    }

    private async Task StartPolling()
    {
        while (true)
        {
            var current = await _clipboard.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(current) && current != _lastSeen)
            {
                _lastSeen = current;

                if (!_historyTexts.Contains(current))
                {
                    _historyTexts.Insert(0, current);
                    if (_historyTexts.Count > 50)
                        _historyTexts.RemoveAt(_historyTexts.Count - 1);
                }
            }

            await Task.Delay(1000);
        }
    }

    public IEnumerable<ListEntry> GetOptions()
        => _historyTexts.Select(text => new ListEntry(text, null));

    public ExecuteResult Execute(string key)
    {
        if (!_historyTexts.Contains(key))
            return new ExecuteResult(false, $"Unknown clipboard entry: {key}");

        // fire-and-forget the async copy
        _ = _clipboard.SetTextAsync(key);
        return new ExecuteResult(true);
    }
}
