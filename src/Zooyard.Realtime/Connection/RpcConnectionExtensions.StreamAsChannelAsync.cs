using System.Threading.Channels;

namespace Zooyard.Realtime.Connection;

/// <summary>
/// Extension methods for <see cref="RpcConnectionExtensions"/>.
/// </summary>
public static partial class RpcConnectionExtensions
{
    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name and return type.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, Array.Empty<object>(), headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and argument.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this RpcConnection hubConnection, string methodName, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 }, headers, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments used to invoke the server method.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static async Task<ChannelReader<TResult>> StreamAsChannelCoreAsync<TResult>(this RpcConnection hubConnection, string methodName, object[] args, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        if (hubConnection == null)
        {
            throw new ArgumentNullException(nameof(hubConnection));
        }

        var inputChannel = await hubConnection.StreamAsChannelCoreAsync(methodName, typeof(TResult), args, headers, cancellationToken);
        var outputChannel = Channel.CreateUnbounded<TResult>();

        // Local function to provide a way to run async code as fire-and-forget
        // The output channel is how we signal completion to the caller.
        async Task RunChannel()
        {
            try
            {
                while (await inputChannel.WaitToReadAsync())
                {
                    while (inputChannel.TryRead(out var item))
                    {
                        while (!outputChannel.Writer.TryWrite((TResult)item))
                        {
                            if (!await outputChannel.Writer.WaitToWriteAsync())
                            {
                                // Failed to write to the output channel because it was closed. Nothing really we can do but abort here.
                                return;
                            }
                        }
                    }
                }

                // Manifest any errors in the completion task
                await inputChannel.Completion;
            }
            catch (Exception ex)
            {
                outputChannel.Writer.TryComplete(ex);
            }
            finally
            {
                // This will safely no-op if the catch block above ran.
                outputChannel.Writer.TryComplete();
            }
        }

        _ = RunChannel();

        return outputChannel.Reader;
    }
}
