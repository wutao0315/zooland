using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Exceptions;
using Zooyard.Atomic;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Processor;
using Zooyard.Loader;
using Zooyard.Rpc.NettyImpl.Hook;
using Zooyard.Rpc.NettyImpl.Constant;

namespace Zooyard.Rpc.NettyImpl.Support
{
    /// <summary>
    /// The type Abstract rpc remoting.
    /// 
    /// </summary>
    public abstract class AbstractNettyRemoting : IAsyncDisposable
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(AbstractNettyRemoting));

        /// <summary>
        /// The Timer executor.
        /// </summary>
        private Task _timerExecutor;
        /// <summary>
        /// The Message executor.
        /// </summary>
        protected internal readonly MultithreadEventLoopGroup messageExecutor;
        /// <summary>
		/// Id generator of this remoting 
        /// </summary>
		protected internal readonly PositiveAtomicCounter idGenerator = new ();
        /// <summary>
        /// The Futures.
        /// </summary>
        protected internal readonly ConcurrentDictionary<int, MessageFuture> _futures = new ();
        //protected internal readonly ConcurrentDictionary<long, TaskCompletionSource<RpcMessage>> _futures = new ConcurrentDictionary<long, TaskCompletionSource<RpcMessage>>();

        private const long NOT_WRITEABLE_CHECK_MILLS = 10L;

        /// <summary>
        /// The Now mills.
        /// </summary>
        protected internal long nowMills = 0L;
        private const int TIMEOUT_CHECK_INTERVAL = 3000;
        private readonly object @lock = new();

        /// <summary>
        /// The Is sending.
        /// </summary>
        protected internal volatile bool isSending = false;

        /// <summary>
		/// This container holds all processors.
		/// processor type <seealso cref="MessageType"/>
		/// </summary>
		protected internal readonly Dictionary<int, Pair<IRemotingProcessor, IExecutorService>> _processorTable = new (32); //MessageType

        protected internal readonly IList<IRpcHook> rpcHooks = EnhancedServiceLoader.LoadAll<IRpcHook>();

        /// <summary>
        /// Init.
        /// </summary>
        public virtual async Task Init()
        {
            //scheduleAtFixedRate
            _timerExecutor = new Task(() =>
            {
                Thread.Sleep(TIMEOUT_CHECK_INTERVAL);
                while (true)
                {
                    foreach (var entry in _futures.ToArray())
                    {
                        if (entry.Value.IsTimeout())
                        {
                            _futures.TryRemove(entry.Key, out _);
                            entry.Value.ResultMessage = null;
                            if (Logger().IsEnabled(LogLevel.Debug))
                            {
                                Logger().LogDebug($"timeout clear future: {entry.Value.RequestMessage.Body}");
                            }
                        }
                    }

                    nowMills = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    Thread.Sleep(TIMEOUT_CHECK_INTERVAL);
                }
            }, TaskCreationOptions.LongRunning);
            _timerExecutor.Start();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Instantiates a new Abstract rpc remoting.
        /// </summary>
        /// <param name="messageExecutor"> the message executor </param>
        public AbstractNettyRemoting(MultithreadEventLoopGroup messageExecutor)//ThreadPoolExecutor messageExecutor)
        {
            this.messageExecutor = messageExecutor;
        }

        /// <summary>
		/// Gets next message id.
		/// </summary>
		/// <returns> the next message id </returns>
		public virtual int NextMessageId
        {
            get
            {
                return idGenerator.IncrementAndGet();
            }
        }
        public virtual ConcurrentDictionary<int, MessageFuture> Futures => _futures;

        public virtual string Group { get; set; } = "DEFAULT";

        public virtual async Task DestroyChannel(IChannel channel)
        {
            await DestroyChannel(ChannelUtil.GetAddressFromChannel(channel), channel);
        }

        /// <summary>
        /// rpc sync request
        /// Obtain the return result through MessageFuture blocking.
        /// </summary>
        /// <param name="channel">       netty channel </param>
        /// <param name="rpcMessage">    rpc message </param>
        /// <param name="timeoutMillis"> rpc communication timeout </param>
        /// <returns> response message </returns>
        /// <exception cref="TimeoutException"> </exception>
        protected internal virtual async Task<object> SendSync(IChannel channel, RpcMessage rpcMessage, long timeoutMillis)
        {
            if (timeoutMillis <= 0)
            {
                throw new FrameworkException("timeout should more than 0ms");
            }
            if (channel == null)
            {
                Logger().LogWarning("sendSync nothing, caused by null channel.");
                return null;
            }

            ChannelWritableCheck(channel, rpcMessage.Body);
            string remoteAddr = ChannelUtil.GetAddressFromChannel(channel);
            DoBeforeRpcHooks(remoteAddr, rpcMessage);

            var messageFuture = new MessageFuture(rpcMessage, TimeSpan.FromMilliseconds(timeoutMillis));
            _futures[rpcMessage.Id] = messageFuture;


            var watch = Stopwatch.StartNew();
            await channel.WriteAndFlushAsync(rpcMessage).ContinueWith(async (future, state) =>
            {
                if (!future.IsCompletedSuccessfully)
                {
                    if (_futures.TryRemove(rpcMessage.Id, out MessageFuture messageFuture1) && messageFuture1 != null)
                    {
                        messageFuture1.ResultMessage = future.Exception;
                    }
                    var channeState = state as IChannel;
                    await DestroyChannel(channeState);
                }
            }, channel);

            try
            {
                object result = messageFuture.Get();
                watch.Stop();
                DoAfterRpcHooks(remoteAddr, rpcMessage, result);
                return result;
            }
            catch (Exception exx)
            {
                Logger().LogError(exx, $"wait response error:{exx.Message},ip:{channel.RemoteAddress},request:{rpcMessage.Body}");
                if (exx is TimeoutException exception)
                {
                    throw exception;
                }
                else
                {
                    throw;
                }
            }
         }

        /// <summary>
        /// rpc async request.
        /// </summary>
        /// <param name="channel">    netty channel </param>
        /// <param name="rpcMessage"> rpc message </param>
        protected internal virtual async Task SendAsync(IChannel channel, RpcMessage rpcMessage)
        {
            ChannelWritableCheck(channel, rpcMessage.Body);
            if (Logger().IsEnabled(LogLevel.Debug))
            {
                Logger().LogDebug($"write message:{rpcMessage.Body}, channel:{channel},active?{channel.Active},writable?{channel.IsWritable},isopen?{channel.Open}");
            }

            DoBeforeRpcHooks(ChannelUtil.GetAddressFromChannel(channel), rpcMessage);

            await channel.WriteAndFlushAsync(rpcMessage).ContinueWith(async (future, state) =>
            {
                if (!future.IsCompletedSuccessfully)
                {
                    var channeState = state as IChannel;
                    await DestroyChannel(channeState);
                }
            }, channel);
        }

        protected internal virtual RpcMessage BuildRequestMessage(object msg, byte messageType)
        {
            var rpcMessage = new RpcMessage
            {
                Id = NextMessageId,
                MessageType = messageType,
                Codec = ProtocolConstants.CONFIGURED_CODEC,
                Compressor = ProtocolConstants.CONFIGURED_COMPRESSOR,
                Body = msg,
            };

            return rpcMessage;
        }

        protected internal virtual RpcMessage BuildResponseMessage(RpcMessage rpcMessage, object msg, byte messageType)
        {
            var rpcMsg = new RpcMessage
            {
                MessageType = messageType,
                Codec = rpcMessage.Codec, // same with request
                Compressor = rpcMessage.Compressor,
                Body = msg,
                Id = rpcMessage.Id
            };
            return rpcMsg;
        }

        /// <summary>
        /// For testing. When the thread pool is full, you can change this variable and share the stack
        /// </summary>
        internal bool allowDumpStack = false;

        /// <summary>
        /// Rpc message processing.
        /// </summary>
        /// <param name="ctx">Channel handler context. </param>
        /// <param name="rpcMessage"> rpc message. </param>
        protected internal virtual void ProcessMessage(IChannelHandlerContext ctx, RpcMessage rpcMessage)
        {
            if (Logger().IsEnabled(LogLevel.Debug))
            {
                Logger().LogDebug($"{this} msgId:{rpcMessage.Id}, body:{rpcMessage.Body}");
            }

            object body = rpcMessage.Body;

            if (body is not IMessageTypeAware messageTypeAware) 
            {
                Logger().LogError($"This rpcMessage body[{body}] is not MessageTypeAware type.");
                return;
            }

            _processorTable.TryGetValue(messageTypeAware.TypeCode, out Pair<IRemotingProcessor, IExecutorService> pair);
            if (pair == null) 
            {
                Logger().LogError($"This message type [{messageTypeAware.TypeCode}] has no processor.");
                return;
            }

            if (pair.Second != null)
            {
                try
                {
                    pair.Second.Execute(() =>
                    {
                        try
                        {
                            pair.First.Process(ctx, rpcMessage);
                        }
                        catch (Exception th)
                        {
                            Logger().LogError(th, $"{FrameworkErrorCode.ExceptionCaught.GetErrCode()}:{th.Message}");
                        }
                    });
                }
                catch (RejectedExecutionException ex) //(Exception ex)
                {
                    Logger().LogError(ex, $"thread pool is full, current max pool size is {messageExecutor.Items.Count()}");// messageExecutor.ActiveCount
                    if (allowDumpStack)
                    {
                        var pid = Environment.ProcessId;
                        int idx = new Random().Next(100);
                        try
                        {
                            using var process = new Process
                            {
                                StartInfo = new ProcessStartInfo("dotnet-dump collect", $"-p {pid} -o {Constants.DefaultDumpDir}{idx}.log")
                                {
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false
                                }
                            };
                            process.Start();
                        }
                        catch (Exception exx)
                        {
                            Logger().LogError(exx, exx.Message);
                        }
                        allowDumpStack = false;
                    }
                }
            }
            else
            {
                try
                {
                    pair.First.Process(ctx, rpcMessage);
                }
                catch (Exception th)
                {
                    Logger().LogError(th, $"NetDispatch:{th.Message}");
                }
            }
        }

        /// <summary>
		/// Gets address from context.
		/// </summary>
		/// <param name="ctx"> the ctx </param>
		/// <returns> the address from context </returns>
		protected internal virtual string GetAddressFromContext(IChannelHandlerContext ctx)
        {
            return ChannelUtil.GetAddressFromChannel(ctx.Channel);
        }

        private void  ChannelWritableCheck(IChannel channel, object msg)
        {
            int tryTimes = 0;
            lock (@lock)
            {
                while (!channel.IsWritable)
                {
                    try
                    {
                        tryTimes++;
                        if (tryTimes > NettyClientConfig.MaxNotWriteableRetry)
                        {
                            DestroyChannel(channel).GetAwaiter().GetResult();
                            throw new FrameworkException($"msg:{((msg == null) ? "null" : msg.ToString())} ChannelIsNotWritable");
                        }
                        Monitor.Wait(@lock, TimeSpan.FromMilliseconds(NOT_WRITEABLE_CHECK_MILLS));
                    }
                    catch (FrameworkException fex)
                    {
                        Logger().LogError(fex, $"{fex.Message}");
                        throw;
                    }
                    catch (Exception exx)
                    {
                        Logger().LogError(exx, exx.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Destroy channel.
        /// </summary>
        /// <param name="serverAddress"> the server address </param>
        /// <param name="channel">       the channel </param>
        public abstract Task DestroyChannel(string serverAddress, IChannel channel);

        protected internal virtual void DoBeforeRpcHooks(string remoteAddr, RpcMessage request)
        {
            foreach (IRpcHook rpcHook in rpcHooks)
            {
                rpcHook.DoBeforeRequest(remoteAddr, request);
            }
        }

        protected internal virtual void DoAfterRpcHooks(string remoteAddr, RpcMessage request, object response)
        {
            foreach (IRpcHook rpcHook in rpcHooks)
            {
                rpcHook.DoAfterResponse(remoteAddr, request, response);
            }
        }

        public virtual async ValueTask DisposeAsync()
        {
            _timerExecutor?.Dispose();
            await messageExecutor.ShutdownGracefullyAsync();
        }
    }
}