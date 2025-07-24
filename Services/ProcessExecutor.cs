using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Scry.Services;
public class ProcessExecutor
{
    private readonly Dictionary<string, ICommandHandler> _handlers;

    public ProcessExecutor()
    {
        var list = new List<ICommandHandler>
        {
            new RunHandler(),
            new ExeHandler(),
            new WebHandler(),
            new ScriptHandler()
        };
        _handlers = list.ToDictionary(h => h.Prefix, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> ValidPrefixes => _handlers.Keys;

    public bool TryGetHandler(string prefix, [NotNullWhen(true)] out ICommandHandler? handler)
        => _handlers.TryGetValue(prefix, out handler);

    public IEnumerable<string> GetOptions(string prefix)
    {
        if (_handlers.TryGetValue(prefix, out var h))
            return h.GetOptions();
        return Array.Empty<string>();
    }

    public ExecuteResult Execute(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new ExecuteResult(false, "Empty command");

        var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return new ExecuteResult(false, "Must be `<prefix> <key>`");

        var prefix = parts[0];
        var key = parts[1];

        if (!_handlers.TryGetValue(prefix, out var handler))
            return new ExecuteResult(false, $"Unknown prefix: {prefix}");

        return handler.Execute(key);
    }
}

