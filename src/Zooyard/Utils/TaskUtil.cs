namespace Zooyard.Utils;

internal class TaskUtil
{
    public static async Task<T> Timeout<T>(Task<T> task, int millisecondsDelay, CancellationTokenSource cts, string errMssage)
    {
        if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)) == task) 
        {
            if (task.Exception != null)
            {
                throw task.Exception;
            }
            return task.Result;
        }

        cts.Cancel();

        throw new TimeoutException(errMssage);
    }

    public static async Task Timeout(Task task, int millisecondsDelay, CancellationTokenSource cts, string errMssage)
    {
        if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)) == task) 
        {
            if (task.Exception!=null) 
            {
                throw task.Exception;
            }
            return;
        }

        cts.Cancel();

        throw new TimeoutException(errMssage);
    }
}
