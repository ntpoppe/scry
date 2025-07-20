namespace Scry.Models;

public class ExecuteResult
{
    public bool Succeeded { get; }
    public string? ErrorMessage { get; }
    public ExecuteResult(bool ok, string? error = null)
    {
        Succeeded = ok;
        ErrorMessage = error;
    }
}
