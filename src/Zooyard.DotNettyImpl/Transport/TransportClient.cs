using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Diagnositcs;
using Zooyard.Exceptions;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Util;
using System.Threading.Channels;
using DotNetty.Transport.Channels;
using System.Net;

namespace Zooyard.DotNettyImpl.Transport
{
    /// <summary>
    /// 一个默认的传输客户端实现。
    /// </summary>
    public class TransportClient : ITransportClient
    {
        private readonly ILogger _logger;
        private readonly IMessageSender _messageSender;
        private readonly IMessageListener _messageListener;

        private readonly ConcurrentDictionary<string, ManualResetValueTaskSource<TransportMessage>> _resultDictionary = new();
        //private readonly DiagnosticListener _diagnosticListener;

        public TransportClient(
        ILogger<TransportClient> logger,
        IMessageSender messageSender, 
        IMessageListener messageListener)
        {
            //_diagnosticListener = new DiagnosticListener(Constant.DiagnosticListenerName);
            _logger = logger;
            _messageSender = messageSender;
            _messageListener = messageListener;
            _messageListener.Received += MessageListener_Received;
        }

        public async Task Open(URL url, CancellationToken cancellationToken)
        {
            await _messageSender.Open(url, cancellationToken);
        }
        /// <summary>
        /// 发送消息。
        /// </summary>
        /// <param name="message">远程调用消息模型。</param>
        /// <returns>远程调用消息的传输消息。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<RemoteInvokeResultMessage> SendAsync(RemoteInvokeMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("准备发送消息。");

                var transportMessage = TransportMessageExtensions.CreateInvokeMessage(message);
                //WirteDiagnosticBefore(transportMessage);
                //注册结果回调
                var callbackTask = RegisterResultCallbackAsync(transportMessage.Id, cancellationToken);

                try
                {
                    //发送
                    await _messageSender.SendAndFlushAsync(transportMessage);
                }
                catch (Exception exception)
                {
                    throw new FrameworkException(exception, "与服务端通讯时发生了异常。");
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("消息发送成功。");

                return await callbackTask;
            }
            catch (Exception exception)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError(exception, "消息发送失败。");
                throw;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //(_messageSender as IDisposable)?.Dispose();
            (_messageListener as IDisposable)?.Dispose();
            foreach (var taskCompletionSource in _resultDictionary.Values)
            {
                taskCompletionSource.SetCanceled();
            }
        }

        /// <summary>
        /// 注册指定消息的回调任务。
        /// </summary>
        /// <param name="id">消息Id。</param>
        /// <returns>远程调用结果消息模型。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<RemoteInvokeResultMessage> RegisterResultCallbackAsync(string id, CancellationToken cancellationToken)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"准备获取Id为：{id}的响应内容。");

            var task = new ManualResetValueTaskSource<TransportMessage>();
            _resultDictionary.TryAdd(id, task);
            try
            {
                var result = await task.AwaitValue(cancellationToken);
                return result.GetContent<RemoteInvokeResultMessage>();
            }
            finally
            {
                //删除回调任务
                if (_resultDictionary.TryRemove(id, out ManualResetValueTaskSource<TransportMessage>? value) && value != null)
                {
                    value.SetCanceled();
                }
            }
        }

        private async Task MessageListener_Received(IMessageSender channel, TransportMessage? message)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("服务消费者接收到消息。");

            if (message == null)
                return;

            if (!_resultDictionary.TryGetValue(message.Id, out ManualResetValueTaskSource<TransportMessage>? task))
                return;

            if (message.IsInvokeResultMessage())
            {
                var content = message.GetContent<RemoteInvokeResultMessage>();
                if (content.Code != 0)
                {
                    task.SetException(new FrameworkException(content.Msg, content.Code));
                }
                else
                {
                    task.SetResult(message);
                }
            }

            if (channel != null && message.IsInvokeMessage())
                await channel.SendAndFlushAsync(new TransportMessage(message));
        }

    }
}
