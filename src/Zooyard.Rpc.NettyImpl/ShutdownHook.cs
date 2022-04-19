using DotNetty.Common.Utilities;
using Zooyard.Atomic;
using Zooyard.Logging;

namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// ensure the shutdownHook is singleton
/// 
/// </summary>
public class ShutdownHook
{

	private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ShutdownHook));

	private static readonly ShutdownHook SHUTDOWN_HOOK = new ("ShutdownHook");

	private readonly PriorityQueue<DisposablePriorityWrapper> disposables = new ();

	private readonly AtomicBoolean destroyed = new(false);

	/// <summary>
	/// default 10. Lower values have higher priority
	/// </summary>
	private const int DEFAULT_PRIORITY = 10;

	static ShutdownHook()
	{
		//Runtime.Runtime.AddShutdownHook(SHUTDOWN_HOOK);
	}

	private ShutdownHook(string name) //: base(name)
	{
	}

	public static ShutdownHook Instance
	{
		get
		{
			return SHUTDOWN_HOOK;
		}
	}

	public virtual void AddDisposable(IAsyncDisposable disposable)
	{
		AddDisposable(disposable, DEFAULT_PRIORITY);
	}

	public virtual void AddDisposable(IAsyncDisposable disposable, int priority)
	{
		disposables.Enqueue(new DisposablePriorityWrapper(disposable, priority));
	}

	public async Task Run()
	{
		await DestroyAll();
	}

	public virtual async Task DestroyAll()
	{
		if (!destroyed.CompareAndSet(false, true))
		{
			return;
		}

		if (disposables.Count<=0)
		{
			return;
		}

		Logger().LogDebug("destoryAll starting");

		while (disposables.Count>0)
		{
			var disposable = disposables.Peek();
			await disposable.DisposeAsync();
		}

		Logger().LogDebug("destoryAll finish");
	}

	/// <summary>
	/// for spring context
	/// </summary>
	public static void RemoveRuntimeShutdownHook()
	{
		//Runtime.Runtime.removeShutdownHook(SHUTDOWN_HOOK);
	}

	private class DisposablePriorityWrapper : IComparable<DisposablePriorityWrapper>, IAsyncDisposable
	{

		internal readonly IAsyncDisposable disposable;

		internal readonly int priority;

		public DisposablePriorityWrapper(IAsyncDisposable disposable, int priority)
		{
			this.disposable = disposable;
			this.priority = priority;
		}

		public virtual int CompareTo(DisposablePriorityWrapper challenger)
		{
			return priority - challenger.priority;
		}

            public async ValueTask DisposeAsync()
            {
			await disposable.DisposeAsync();
		}
        }
}
