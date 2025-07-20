using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Scry.Services;

public class ProcessLauncher : IProcessLauncher
{
    private List<string> _validPrefixes = new()
    {
        "run"
    };


    public LaunchResult Launch(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new LaunchResult(false, "Empty command");

        var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return new LaunchResult(false, "Invalid part length");

        var prefix = parts[0].ToLowerInvariant();
        var key = parts[1].ToLowerInvariant();

        if (!_validPrefixes.Contains(prefix))
            return new LaunchResult(false, "Invalid prefix");

        switch (prefix)
        {
            case "run":
                return HandleRun(key);
            default:
                throw new InvalidOperationException("invalid prefix");
        }
    }

    private LaunchResult HandleRun(string key)
    {
        var path = key switch
        {
            "notepad" => "notepad.exe",
            _ => null
        };

        if (path is null)
            return new LaunchResult(false, "Path not found");

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return new LaunchResult(true);
        }
        catch (Exception ex)
        {
            return new LaunchResult(false, ex.Message);
        }
    }
}
