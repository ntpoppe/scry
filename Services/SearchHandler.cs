using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Scry.Services;

public class SearchHandler : ICommandHandler
{
    public string Prefix => "search";

    public string Description => "search the web";

    public bool IsEntryless => true;

    public IEnumerable<ListEntry> GetOptions()
        => Enumerable.Empty<ListEntry>();

    public ExecuteResult Execute(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new ExecuteResult(false, "Search query cannot be empty");

        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = $"https://www.google.com/search?q={encodedQuery}";

            Process.Start(new ProcessStartInfo(searchUrl) { UseShellExecute = true });

            return new ExecuteResult(true);
        }
        catch (Exception ex)
        {
            return new ExecuteResult(false, $"Failed to open browser: {ex.Message}");
        }
    }
}
