using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Zooyard;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyInvoker : AbstractInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyInvoker));

        private readonly IChannel _channel;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>> _resultDictionary =
            new ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>>();
        private readonly int _clientTimeout;

        public NettyInvoker(IChannel channel, IMessageListener _messageListener, int clientTimeout)
        {
            _channel = channel;
            _clientTimeout = clientTimeout;
            _messageListener.Received += MessageListener_Received;
        }
        public override object Instance => _channel;
        public override int ClientTimeout => _clientTimeout;

        protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
        {
            var message = new RemoteInvokeMessage
            {
                Method = invocation.MethodInfo.Name,
                Arguments = invocation.Arguments
            };

            var transportMessage = TransportMessage.CreateInvokeMessage(message);

            //注册结果回调
            var callbackTask = RegisterResultCallbackAsync(transportMessage.Id);

            var watch = Stopwatch.StartNew();
            try
            {
                var bytes = transportMessage.Serialize();
                var byteBuffers = Unpooled.WrappedBuffer(bytes);
                await _channel.WriteAndFlushAsync(byteBuffers);

                if (callbackTask.Wait(ClientTimeout / 2))
                {
                    var value = await callbackTask;

                    watch.Stop();
                    if (invocation.MethodInfo.ReturnType == typeof(Task))
                    {
                        return new RpcResult<T>(watch.ElapsedMilliseconds);
                        //return new RpcResult(Task.CompletedTask);
                    }
                    else if (invocation.MethodInfo.ReturnType.IsGenericType && invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        //var resultData = Task.FromResult((dynamic)value.Result);
                        return new RpcResult<T>((T)value.Result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
                    }

                    return new RpcResult<T>((T)value.Result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
                }
                else
                {
                    throw new TimeoutException($"connection time out in {ClientTimeout} ms");
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.StackTrace);
                throw;
            }
            finally
            {
                if (watch.IsRunning)
                    watch.Stop();
                Logger().LogInformation($"Thrift Invoke {watch.ElapsedMilliseconds} ms");
            }
        }


        /// <summary>
        /// 注册指定消息的回调任务。
        /// </summary>
        /// <param name="id">消息Id。</param>
        /// <returns>远程调用结果消息模型。</returns>
        private async Task<RemoteInvokeResultMessage> RegisterResultCallbackAsync(string id)
        {
            Logger().LogDebug($"ready to recive message Id：{id} response content。");

            var task = new TaskCompletionSource<TransportMessage>();
            _resultDictionary.TryAdd(id, task);
            try
            {
                var result = await task.Task;
                return result.GetContent<RemoteInvokeResultMessage>();
            }
            catch (Exception e)
            {
                Logger().LogError(e, e.Message);
                return null;
            }
            finally
            {
                //删除回调任务
                _resultDictionary.TryRemove(id, out TaskCompletionSource<TransportMessage> value);
            }
        }

        private async Task MessageListener_Received(TransportMessage message)
        {
            Logger().LogInformation($"serivce customer recive the message:{message.Id} ");

            if (!_resultDictionary.TryGetValue(message.Id, out TaskCompletionSource<TransportMessage> task))
                return;

            if (message.IsInvokeResultMessage())
            {
                var content = message.GetContent<RemoteInvokeResultMessage>();

                message.Content = content;
                if (!string.IsNullOrEmpty(content.ExceptionMessage))
                {
                    task.TrySetException(new Exception($"{content.ExceptionMessage};statusCode :{content.StatusCode}"));
                }
                else
                {
                    task.SetResult(message);
                }
            }
            await Task.CompletedTask;
        }
    }
}
