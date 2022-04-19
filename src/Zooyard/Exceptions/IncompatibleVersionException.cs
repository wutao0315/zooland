namespace Zooyard.Exceptions;

/// <summary>
/// The type Incompatible version exception.
/// 
/// </summary>
public class IncompatibleVersionException : Exception
{

	/// <summary>
	/// Instantiates a new Incompatible version exception.
	/// </summary>
	/// <param name="message"> the message </param>
	public IncompatibleVersionException(string message) : base(message)
	{
	}
}
