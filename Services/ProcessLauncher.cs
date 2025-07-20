using System.Diagnostics;

namespace Scry.Services;

public class ProcessLauncher : IProcessLauncher
{
    public void Launch(string pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
            return;

        var psi = new ProcessStartInfo(pathOrUrl)
        {
            UseShellExecute = true
        };

        Process.Start(psi);
    }
}
