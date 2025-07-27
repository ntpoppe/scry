using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Scry.Services;

public class WebHandler : ICommandHandler
{
    public string Prefix => "web";
    public string Description => "open webpage";
    public bool IsEntryless => false;

    private readonly Dictionary<string, string> _map = new()
    {
        ["youtube"] = "https://www.youtube.com",
        ["gmail"] = "https://mail.google.com",
        ["google"] = "https://www.google.com",
        ["github"] = "https://github.com",
        ["gitlab"] = "https://gitlab.com",
        ["stackoverflow"] = "https://stackoverflow.com",
        ["reddit"] = "https://www.reddit.com",
        ["linkedin"] = "https://www.linkedin.com",
        ["twitter"] = "https://twitter.com",
        ["facebook"] = "https://www.facebook.com",
        ["amazon"] = "https://www.amazon.com",
        ["outlook"] = "https://outlook.office.com",
        ["azure devops"] = "https://dev.azure.com",
        ["docs"] = "https://docs.microsoft.com",
        ["drive"] = "https://drive.google.com",
        ["calendar"] = "https://calendar.google.com",
        ["notion"] = "https://www.notion.so",
        ["chatgpt"] = "https://www.chatgpt.com",
        ["perplexity"] = "https://www.perplexity.ai",
        ["gemini"] = "https://www.gemini.google.com",
        ["claude"] = "https://www.claude.ai",
        ["facebook"] = "https://www.facebook.com",
        ["twitter"] = "https://www.x.com"
    };

    public IEnumerable<ListEntry> GetOptions()
        => _map
           .DistinctBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
           .Select(kvp => new ListEntry(kvp.Key, null));

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
