namespace Zooyard.Utils;

internal class TaskUtil
{
    public static async Task<T> Timeout<T>(Task<T> task, int millisecondsDelay, CancellationTokenSource cts, string errMssage)
    {
        if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)) == task)
            return task.Result;

        cts.Cancel();

        throw new TimeoutException(errMssage);
    }

    public static async Task Timeout(Task task, int millisecondsDelay, CancellationTokenSource cts, string errMssage)
    {
        if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)) == task)
            return;

        cts.Cancel();

        throw new TimeoutException(errMssage);
    }
}
