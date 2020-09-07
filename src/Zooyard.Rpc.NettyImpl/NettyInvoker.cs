using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyInvoker : AbstractInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyInvoker));

        private readonly IChannel _channel;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>> _resultDictionary =
            new ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>>();
        private readonly int _clientTimeout = 5000;

        public NettyInvoker(IChannel channel,IMessageListener _messageListener)
        {
            _channel = channel;
            _messageListener.Received += MessageListener_Received;
        }
        public override object Instance { get { return _channel; } }
        protected override async Task<IResult> HandleInvoke(IInvocation invocation)
        {
            try
            {
                var message = new RemoteInvokeMessage
                {
                    Method = invocation.MethodInfo.Name,
                    Arguments = invocation.Arguments
                };

                var transportMessage = TransportMessage.CreateInvokeMessage(message);
                
                //注册结果回调
                var callbackTask = RegisterResultCallbackAsync(transportMessage.Id);

                try
                {
                    var bytes = transportMessage.Serialize();
                    var byteBuffers = Unpooled.WrappedBuffer(bytes);

                    await _channel.WriteAndFlushAsync(byteBuffers);
                }
                catch (Exception e)
                {
                    Logger().Error(e, e.Message);
                    throw new Exception("connecting server error.", e);
                }

                Logger().Information($"Invoke:{invocation.MethodInfo.Name}");
                if (callbackTask.Wait(_clientTimeout / 2))
                {
                    var value = await callbackTask;
                    return new RpcResult(value.Result);
                }
                else
                {
                    throw new TimeoutException($"connection time out in {_clientTimeout} ms");
                }
            }
            catch (Exception e)
            {
                Logger().Error(e,e.Message);
                throw e;
            }
        }


        /// <summary>
        /// 注册指定消息的回调任务。
        /// </summary>
        /// <param name="id">消息Id。</param>
        /// <returns>远程调用结果消息模型。</returns>
        private async Task<RemoteInvokeResultMessage> RegisterResultCallbackAsync(string id)
        {
            Logger().Debug($"ready to recive message Id：{id} response content。");

            var task = new TaskCompletionSource<TransportMessage>();
            _resultDictionary.TryAdd(id, task);
            try
            {
                var result = await task.Task;
                return result.GetContent<RemoteInvokeResultMessage>();
            }
            catch (Exception e)
            {
                Logger().Error(e, e.Message);
                return null;
            }
            finally
            {
                //删除回调任务
                TaskCompletionSource<TransportMessage> value;
                _resultDictionary.TryRemove(id, out value);
            }
        }

        private async Task MessageListener_Received(TransportMessage message)
        {
            Logger().Information($"serivce customer recive the message:{message.Id} ");


            TaskCompletionSource<TransportMessage> task;
            if (!_resultDictionary.TryGetValue(message.Id, out task))
                return;

            if (message.IsInvokeResultMessage())
            {
                var content = message.GetContent<RemoteInvokeResultMessage>();

                message.Content = content;
                if (!string.IsNullOrEmpty(content.ExceptionMessage))
                {
                    task.TrySetException(new Exception($"{content.ExceptionMessage};statusCode :{content.StatusCode}" ));
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
