namespace Zooyard.Loader;

public class EnhancedServiceNotFoundException: Exception
{
    /// <summary>
		/// Instantiates a new Enhanced service not found exception.
		/// </summary>
		/// <param name="errorCode"> the error code </param>
		public EnhancedServiceNotFoundException(string errorCode) : base(errorCode)
    {
    }

    /// <summary>
    /// Instantiates a new Enhanced service not found exception.
    /// </summary>
    /// <param name="errorCode"> the error code </param>
    /// <param name="cause">     the cause </param>
    public EnhancedServiceNotFoundException(string errorCode, Exception cause) : base(errorCode, cause)
    {
    }

    /// <summary>
    /// Instantiates a new Enhanced service not found exception.
    /// </summary>
    /// <param name="errorCode"> the error code </param>
    /// <param name="errorDesc"> the error desc </param>
    public EnhancedServiceNotFoundException(string errorCode, string errorDesc) : base(errorCode + ":" + errorDesc)
    {
    }

    /// <summary>
    /// Instantiates a new Enhanced service not found exception.
    /// </summary>
    /// <param name="errorCode"> the error code </param>
    /// <param name="errorDesc"> the error desc </param>
    /// <param name="cause">     the cause </param>
    public EnhancedServiceNotFoundException(string errorCode, string errorDesc, Exception cause) : base(errorCode + ":" + errorDesc, cause)
    {
    }

    /// <summary>
    /// Instantiates a new Enhanced service not found exception.
    /// </summary>
    /// <param name="cause"> the cause </param>
    public EnhancedServiceNotFoundException(Exception cause) : base(cause.Message, cause)
    {
    }

}
