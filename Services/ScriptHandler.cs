using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Scry.Services;

public class ScriptHandler : ICommandHandler
{
    public string Prefix => "script";
    public string Description => "placeholder, not implemented";
    public bool IsEntryless => false;

    private readonly string _scriptsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScryScripts");

    public IEnumerable<ListEntry> GetOptions()
    {
        if (!Directory.Exists(_scriptsFolder))
            return Array.Empty<ListEntry>();

        return Directory
            .EnumerateFiles(_scriptsFolder)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new ListEntry(name!, null));
    }

    public ExecuteResult Execute(string key)
    {
        var file = Path.Combine(_scriptsFolder, key + ".bat"); // or .ps1, etc
        if (!File.Exists(file))
            return new ExecuteResult(false, $"Script not found: {key}");
        try
        {
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            return new ExecuteResult(true);
        }
        catch (Exception ex)
        {
            return new ExecuteResult(false, ex.Message);
        }
    }
}
