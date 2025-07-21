using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Scry.Services;

public class WebHandler : ICommandHandler
{
    public string Prefix => "web";

    private readonly Dictionary<string, string> _map = new()
    {
        ["chatgpt"] = "https://chatgpt.com",
        ["youtube"] = "https://youtube.com",
    };

    public IEnumerable<string> GetOptions() => _map.Keys;

    public ExecuteResult Execute(string key)
    {
        if (!_map.TryGetValue(key, out var url))
            return new ExecuteResult(false, $"Unknown site: {key}");

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return new ExecuteResult(true);
        }
        catch (Exception ex)
        {
            return new ExecuteResult(false, ex.Message);
        }
    }
}
