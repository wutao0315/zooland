﻿using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyInvoker : IInvoker
    {
        private readonly IChannel _client;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>> _resultDictionary =
            new ConcurrentDictionary<string, TaskCompletionSource<TransportMessage>>();

        public NettyInvoker(IChannel client,IMessageListener _messageListener,ILoggerFactory loggerFactory)
        {
            _client = client;
            _messageListener.Received += MessageListener_Received;
            _logger = loggerFactory.CreateLogger<NettyInvoker>();
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
                    var bytes = transportMessage.Serialize();
                    var byteBuffers = Unpooled.WrappedBuffer(bytes);

                    _client.WriteAndFlushAsync(byteBuffers).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    throw new Exception("connecting server error.", e);
                }

                var value = callbackTask.GetAwaiter().GetResult();
                _logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
                return new RpcResult(value.Result);
                

            }
            catch (Exception e)
            {
                _logger.LogError(e,e.Message);
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
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"ready to recive message Id：{id} response content。");
            }

            var task = new TaskCompletionSource<TransportMessage>();
            _resultDictionary.TryAdd(id, task);
            try
            {
                var result = await task.Task;
                return result.GetContent<RemoteInvokeResultMessage>();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
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
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"serivce customer recive the message:{message.Id} ");
            }
                

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

        }
    }
}
