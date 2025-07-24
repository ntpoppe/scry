using Scry.Models;
using System.Collections.Generic;

namespace Scry.Services;

public interface ICommandHandler
{
    /// <summary>
    /// The literal prefix, e.g. "run", "web", "script".
    /// </summary>
    string Prefix { get; }

    /// <summary>
    /// The description of the handler.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// All the things this handler can run (for filtering/autocomplete).
    /// </summary>
    IEnumerable<ListEntry> GetOptions();

    /// <summary>
    /// Execute the given key (which must be one of GetOptions()).
    /// </summary>
    ExecuteResult Execute(string key);
}
