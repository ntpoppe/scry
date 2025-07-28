using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Scry.Services;

public class ExeHandler : ICommandHandler
{
    public string Prefix => "exe";
    public string Description => "launch executables from PATH";
    public bool IsEntryless => false;

    private Lazy<List<ListEntry>> _executables = new(() =>
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? "")
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return paths
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.exe"))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new ListEntry(name!, null))
            .ToList();
    });

    public IEnumerable<ListEntry> GetOptions() => _executables.Value;

    public ExecuteResult Execute(string key)
    {
        var match = _executables.Value
            .FirstOrDefault(e =>
                string.Equals(e.Value, key, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return new ExecuteResult(false, $"Unknown executable: {key}");

        try
        {
            Process.Start(new ProcessStartInfo(match.Value) { UseShellExecute = true });
            return new ExecuteResult(true);
        }
        catch (Exception ex)
        {
            return new ExecuteResult(false, ex.Message);
        }
    }
}
