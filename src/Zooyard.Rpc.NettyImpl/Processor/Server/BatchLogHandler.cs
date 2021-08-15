using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Zooyard.Logging;
using System.Threading;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;

namespace Zooyard.Rpc.NettyImpl.Processor.Server
{
	/// <summary>
	/// handle ServerOnRequestProcessor and ServerOnResponseProcessor log print.
	/// 
	/// </summary>
	public class BatchLogHandler
	{
		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(BatchLogHandler));
		private static readonly BlockingCollection <string> LOG_QUEUE = new ();
		public static readonly BatchLogHandler INSTANCE = new ();

		private const int MAX_LOG_SEND_THREAD = 1;
		private const int MAX_LOG_TAKE_SIZE = 1024;
		//private const long KEEP_ALIVE_TIME = 0L;
		//private const string THREAD_PREFIX = "batchLoggerPrint";
		private const int BUSY_SLEEP_MILLS = 5;

		public static CancellationToken CancellationToken = new ();

		static BatchLogHandler()
		{

            //IExecutorService mergeSendExecutorService = new ExecutorTaskScheduler() SingleThreadEventExecutor(THREAD_PREFIX,TimeSpan.FromMilliseconds(KEEP_ALIVE_TIME));
            //ThreadPoolExecutor(MAX_LOG_SEND_THREAD, MAX_LOG_SEND_THREAD, KEEP_ALIVE_TIME, TimeUnit.MILLISECONDS, new LinkedBlockingQueue<>(), new NamedThreadFactory(THREAD_PREFIX, MAX_LOG_SEND_THREAD, true));
            //mergeSendExecutorService.SubmitAsync(() => new BatchLogRunnable());
            IExecutorService mergeSendExecutorService = new MultithreadEventLoopGroup(MAX_LOG_SEND_THREAD);
            mergeSendExecutorService.SubmitAsync(() => new BatchLogRunnable().Run());
        }

		public virtual BlockingCollection<string> LogQueue => LOG_QUEUE;

		/// <summary>
		/// The type Batch log runnable.
		/// </summary>
		internal class BatchLogRunnable
		{
			public bool Run()
			{
				var logList = new List<string>();
				while (true)
				{
					if (CancellationToken.IsCancellationRequested) 
					{
						break;
					}
					try
					{
						logList.Add(LOG_QUEUE.Take());
                        //LOG_QUEUE.drainTo(logList, MAX_LOG_TAKE_SIZE);
                        for (int i = 0; i < MAX_LOG_TAKE_SIZE; i++)
                        {
							if (!LOG_QUEUE.TryTake(out string item)) 
							{
								break;
							}
							logList.Add(item);
						}
						if (Logger().IsEnabled(LogLevel.Debug))
						{
							foreach (string str in logList)
							{
								Logger().LogInformation(str);
							}
						}
						logList.Clear();
						Thread.Sleep(BUSY_SLEEP_MILLS);
						//TimeUnit.MILLISECONDS.sleep(BUSY_SLEEP_MILLS);
					}
					catch (Exception exx)
					{
						Logger().LogError(exx, $"batch log busy sleep error:{exx.Message}");
					}
				}
				return true;
			}
		}

	}

}