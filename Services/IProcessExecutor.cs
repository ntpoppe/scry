namespace Scry.Services;

public interface IProcessExecutor
{
    ExecuteResult Execute(string pathOrUrl);
}
