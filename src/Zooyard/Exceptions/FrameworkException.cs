using System;
using Zooyard.Logging;

namespace Zooyard.Exceptions
{
	/// <summary>
	/// The type Framework exception.
	/// 
	/// </summary>
	public class FrameworkException : Exception
	{
		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(FrameworkException));
		private readonly FrameworkErrorCode errcode;
		/// <summary>
		/// Instantiates a new Framework exception.
		/// </summary>
		public FrameworkException() : this(FrameworkErrorCode.UnknownAppError)
		{
		}

		/// <summary>
		/// Instantiates a new Framework exception.
		/// </summary>
		/// <param name="err"> the err </param>
		public FrameworkException(FrameworkErrorCode err) : this(err.GetErrMessage(), err)
		{
		}

		/// <summary>
		/// Instantiates a new Framework exception.
		/// </summary>
		/// <param name="msg"> the msg </param>
		public FrameworkException(string msg) : this(msg, FrameworkErrorCode.UnknownAppError)
		{
		}

		/// <summary>
		/// Instantiates a new Framework exception.
		/// </summary>
		/// <param name="msg">     the msg </param>
		/// <param name="errCode"> the err code </param>
		public FrameworkException(string msg, FrameworkErrorCode errCode) : this(null, msg, errCode)
		{
		}

		/// <summary>
		/// Instantiates a new Framework exception.
		/// </summary>
		/// <param name="cause">   the cause </param>
		/// <param name="msg">     the msg </param>
		/// <param name="errCode"> the err code </param>
		public FrameworkException(Exception cause, string msg, FrameworkErrorCode errCode) : base(msg, cause)
		{
			this.errcode = errCode;
		}

		/// <summary>
		/// Instantiates a new Framework exception.
		/// </summary>
		/// <param name="th"> the th </param>
		public FrameworkException(Exception th) : this(th, th.Message)
		{
		}

		/// <summary>
		/// Instantiates a new Framework exception.
		/// </summary>
		/// <param name="th">  the th </param>
		/// <param name="msg"> the msg </param>
		public FrameworkException(Exception th, string msg) : this(th, msg, FrameworkErrorCode.UnknownAppError)
		{
		}

		/// <summary>
		/// Gets errcode.
		/// </summary>
		/// <returns> the errcode </returns>
		public virtual FrameworkErrorCode Errcode
		{
			get
			{
				return errcode;
			}
		}

		/// <summary>
		/// Nested exception framework exception.
		/// </summary>
		/// <param name="e"> the e </param>
		/// <returns> the framework exception </returns>
		public static FrameworkException NestedException(Exception e)
		{
			return NestedException("", e);
		}

		/// <summary>
		/// Nested exception framework exception.
		/// </summary>
		/// <param name="msg"> the msg </param>
		/// <param name="e">   the e </param>
		/// <returns> the framework exception </returns>
		public static FrameworkException NestedException(string msg, Exception e)
		{
			Logger().LogError(e, msg + e.Message);
			if (e is FrameworkException exception)
			{
				return exception;
			}

			return new FrameworkException(e, msg);
		}

		/// <summary>
		/// Nested sql exception sql exception.
		/// </summary>
		/// <param name="e"> the e </param>
		/// <returns> the sql exception </returns>
		public static Exception NestedSQLException(Exception e)
		{
			return NestedSQLException("", e);
		}

		/// <summary>
		/// Nested sql exception sql exception.
		/// </summary>
		/// <param name="msg"> the msg </param>
		/// <param name="e">   the e </param>
		/// <returns> the sql exception </returns>
		public static Exception NestedSQLException(string msg, Exception e)
		{
			Logger().LogError(e, msg + e.Message);
			return e;
		}
	}

}