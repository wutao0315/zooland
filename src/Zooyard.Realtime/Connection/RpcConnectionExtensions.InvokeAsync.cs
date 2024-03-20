namespace Zooyard.Realtime.Connection;

/// <summary>
/// Extension methods for <see cref="RpcConnectionExtensions"/>.
/// </summary>
public partial class RpcConnectionExtensions
{
    /// <summary>
    /// Invokes a hub method on the server using the specified method name.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, Array.Empty<object>(), headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and argument.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3, arg4], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3, arg4, arg5], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3, arg4, arg5, arg6], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3, arg4, arg5, arg6, arg7], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    /// <param name="arg10">The tenth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeAsync(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.InvokeCoreAsync(methodName, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10], headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments used to invoke the server method.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public static Task InvokeCoreAsync(this RpcConnection hubConnection, string methodName, object[] args, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        if (hubConnection == null)
        {
            throw new ArgumentNullException(nameof(hubConnection));
        }

        return hubConnection.InvokeCoreAsync(methodName, typeof(object), args, headers, cancellationToken);
    }
}
