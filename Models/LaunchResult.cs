namespace Scry.Models;

public class LaunchResult
{
    public bool Succeeded { get; }
    public string? ErrorMessage { get; }
    public LaunchResult(bool ok, string? error = null)
    {
        Succeeded = ok;
        ErrorMessage = error;
    }
}
