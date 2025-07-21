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

    private readonly string _scriptsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScryScripts");

    public IEnumerable<string> GetOptions()
    {
        if (!Directory.Exists(_scriptsFolder))
            return Array.Empty<string>();

        return Directory
            .EnumerateFiles(_scriptsFolder)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!);
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
