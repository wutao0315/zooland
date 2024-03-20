namespace Zooyard.Realtime.Server;

internal static class TaskCache
{
    public static readonly Task<bool> True = Task.FromResult(true);
    public static readonly Task<bool> False = Task.FromResult(false);
}
