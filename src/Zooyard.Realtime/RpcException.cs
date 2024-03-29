﻿using System.Runtime.Serialization;

namespace Zooyard.Realtime;

/// <summary>
/// The exception thrown from a hub when an error occurs.
/// </summary>
[Serializable]
public class RpcException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RpcException"/> class.
    /// </summary>
    public RpcException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public RpcException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
    public RpcException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcException"/> class.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is <c>null</c>.</exception>
    /// <exception cref="SerializationException">The class name is <c>null</c> or <see cref="Exception.HResult"/> is zero (0).</exception>
    public RpcException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
