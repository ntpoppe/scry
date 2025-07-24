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

    private Lazy<List<string>> _executables = new(() =>
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? "")
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return paths
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.exe"))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    });

    public IEnumerable<string> GetOptions() => _executables.Value;

    public ExecuteResult Execute(string key)
    {
        if (!_executables.Value.Contains(key, StringComparer.OrdinalIgnoreCase))
            return new ExecuteResult(false, $"Unknown executable: {key}");

        try
        {
            Process.Start(new ProcessStartInfo(key + ".exe") { UseShellExecute = true });
            return new ExecuteResult(true);
        }
        catch (Exception ex)
        {
            return new ExecuteResult(false, ex.Message);
        }
    }
}
