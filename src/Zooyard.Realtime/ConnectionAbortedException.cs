﻿namespace Zooyard.Realtime;

/// <summary>
/// An exception that is thrown when a connection is aborted by the server.
/// </summary>
public class ConnectionAbortedException : OperationCanceledException
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionAbortedException"/>.
    /// </summary>
    public ConnectionAbortedException() :
        this("The connection was aborted")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionAbortedException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ConnectionAbortedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionAbortedException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="inner">The underlying <see cref="Exception"/>.</param>
    public ConnectionAbortedException(string message, Exception inner) : base(message, inner)
    {
    }
}