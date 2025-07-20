using Scry.Models;

namespace Scry.Services;

public interface IProcessLauncher
{
    LaunchResult Launch(string pathOrUrl);
}
