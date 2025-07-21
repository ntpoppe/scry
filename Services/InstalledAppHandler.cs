using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Scry.Services;

public class InstalledAppHandler : ICommandHandler
{
    public string Prefix => "app";

    private List<string>? _cache;

    private static readonly string[] StartMenuPaths = new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
    };

    public IEnumerable<string> GetOptions()
    {
        // If cached, return it directly
        if (_cache is not null)
            return _cache;

        // Not Windows? nothing to do
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Enumerable.Empty<string>();

        // Try to get the WSH COM type
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
            return Enumerable.Empty<string>();

        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null)
            return Enumerable.Empty<string>();

        // Enumerate shortcuts once
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in StartMenuPaths)
        {
            if (!Directory.Exists(root)) continue;
            foreach (var lnk in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
            {
                try
                {
                    dynamic? shortcut = shell.CreateShortcut(lnk);
                    if (shortcut?.TargetPath is object)
                        names.Add(Path.GetFileNameWithoutExtension(lnk)!);
                }
                catch { }
            }
        }

        // Cache and return
        _cache = names.OrderBy(n => n).ToList();
        return _cache;
    }

    public ExecuteResult Execute(string key)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new ExecuteResult(false, "App launching only supported on Windows");

        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
            return new ExecuteResult(false, "WScript.Shell COM not available");

        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null)
            return new ExecuteResult(false, "Failed to create WshShell");

        foreach (var root in StartMenuPaths)
        {
            foreach (var lnk in Directory.EnumerateFiles(root, $"{key}.lnk", SearchOption.AllDirectories))
            {
                try
                {
                    dynamic? shortcut = shell.CreateShortcut(lnk);
                    string? target = shortcut?.TargetPath;
                    if (!string.IsNullOrWhiteSpace(target))
                    {
                        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                        return new ExecuteResult(true);
                    }
                }
                catch (Exception ex)
                {
                    return new ExecuteResult(false, ex.Message);
                }
            }
        }

        return new ExecuteResult(false, $"Shortcut for “{key}” not found");
    }
}
