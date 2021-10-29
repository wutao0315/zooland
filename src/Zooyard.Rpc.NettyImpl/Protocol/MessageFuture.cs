using System;
using System.Threading.Tasks;


namespace Zooyard.Rpc.NettyImpl.Protocol
{

	/// <summary>
	/// The type Message future.
	/// 
	/// </summary>
	public class MessageFuture
	{
		private readonly DateTime start = DateTime.Now;

		public TaskCompletionSource<object> Origin = new ();
		public MessageFuture(RpcMessage reqeustMessage, TimeSpan timeout) 
		{
			RequestMessage = reqeustMessage;
			Timeout = timeout;
		}
		/// <summary>
		/// Is timeout boolean.
		/// </summary>
		/// <returns> the boolean </returns>
		public virtual bool IsTimeout()
		{
			return DateTime.Now - start > Timeout;
		}

		/// <summary>
		/// Get object.
		/// </summary>
		/// <returns> the object </returns>
		/// <exception cref="TimeoutException"> the timeout exception </exception>
		public virtual object Get()
		{
            object result;
            try
			{
                Task.WaitAll(new[] { Origin.Task }, Timeout);
                result = Origin.Task.Result;
			}
			catch (TimeoutException)
			{
				throw new TimeoutException("cost " + (DateTime.Now - start).TotalMilliseconds + " ms");
			}

			if (result is Exception exception)
			{
				throw exception;
			}

			return result;
		}

		/// <summary>
		/// Sets result message.
		/// </summary>
		/// <param name="obj"> the obj </param>
		public virtual object ResultMessage
		{
			set
			{
                Origin.SetResult(value);
			}
		}

		/// <summary>
		/// Gets request message.
		/// </summary>
		/// <returns> the request message </returns>
		public virtual RpcMessage RequestMessage { get; }
        
        /// <summary>
        /// Gets timeout.
        /// </summary>
        /// <returns> the timeout </returns>
        public virtual TimeSpan Timeout { get; }
	}
}