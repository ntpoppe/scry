using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Scry.Services;

public class ProcessExecutor : IProcessExecutor
{
    private List<string> _validPrefixes = new()
    {
        "run"
    };

    public ExecuteResult Execute(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new ExecuteResult(false, "Empty command");

        var parts = command.TrimEnd().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return new ExecuteResult(false, "Invalid part length");

        var prefix = parts[0].ToLowerInvariant();
        var key = parts[1].ToLowerInvariant();

        if (!_validPrefixes.Contains(prefix))
            return new ExecuteResult(false, "Invalid prefix");

        switch (prefix)
        {
            case "run":
                return HandleRun(key);
            default:
                throw new InvalidOperationException("invalid prefix");
        }
    }

    private ExecuteResult HandleRun(string key)
    {
        var path = key switch
        {
            "notepad" => "notepad.exe",
            _ => null
        };

        if (path is null)
            return new ExecuteResult(false, "Path not found");

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return new ExecuteResult(true);
        }
        catch (Exception ex)
        {
            return new ExecuteResult(false, ex.Message);
        }
    }
}
