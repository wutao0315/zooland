using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyInvoker : IInvoker
    {
        private IChannel client { get; set; }
        private readonly ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>> _resultDictionary =
            new ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>>();

        public NettyInvoker(IChannel client,IMessageListener _messageListener)
        {
            this.client = client;
            _messageListener.Received += MessageListener_Received;
        }

        public IResult Invoke(IInvocation invocation)
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

                    //发送
                    //var bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(transportMessage));
                    var bytes = transportMessage.Serialize();
                    var byteBuffers = Unpooled.WrappedBuffer(bytes);

                    client.WriteAndFlushAsync(byteBuffers).GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    throw new Exception("与服务端通讯时发生了异常。", exception);
                }

                var value = callbackTask.GetAwaiter().GetResult();
                return new RpcResult(value.Result);

                //if (invocation.MethodInfo.ReturnType.IsValueType)
                //{
                //    if (invocation.MethodInfo.ReturnType == typeof(void))
                //    {
                //        return new RpcResult();
                //    }
                //    return new RpcResult(value.Result.ChangeType(invocation.MethodInfo.ReturnType));
                //}

                //if (invocation.MethodInfo.ReturnType == typeof(string))
                //{
                //    return new RpcResult(value.Result);
                //}

                //var result = new RpcResult(JsonConvert.DeserializeObject(value.Result.ToString(), invocation.MethodInfo.ReturnType));
                //return result;

            }
            catch (Exception exception)
            {
                throw exception;
            }


            
        }


        /// <summary>
        /// 注册指定消息的回调任务。
        /// </summary>
        /// <param name="id">消息Id。</param>
        /// <returns>远程调用结果消息模型。</returns>
        private async Task<RemoteInvokeResultMessage> RegisterResultCallbackAsync(string id)
        {
            //if (_logger.IsEnabled(LogLevel.Debug))
            //    _logger.LogDebug($"准备获取Id为：{id}的响应内容。");

            var task = new TaskCompletionSource<TransportMessage>();
            _resultDictionary.TryAdd(id, task);
            try
            {
                var result = await task.Task;
                return result.GetContent<RemoteInvokeResultMessage>();
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
            //if (_logger.IsEnabled(LogLevel.Trace))
            //    _logger.LogTrace("服务消费者接收到消息。");

            TaskCompletionSource<TransportMessage> task;
            if (!_resultDictionary.TryGetValue(message.Id, out task))
                return;

            if (message.IsInvokeResultMessage())
            {
                var content = message.GetContent<RemoteInvokeResultMessage>();
                //var content = JsonConvert.DeserializeObject<RemoteInvokeResultMessage>(message.Content.ToString());
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
            //if (_serviceExecutor != null && message.IsInvokeMessage())
            //    await _serviceExecutor.ExecuteAsync(sender, message);
        }
    }
}
