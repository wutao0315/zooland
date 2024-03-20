using Microsoft.AspNetCore.Http;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// A helper for creating the response Set-Cookie header.
/// </summary>
public interface IResponseCookiesFeature
{
    /// <summary>
    /// Gets the wrapper for the response Set-Cookie header.
    /// </summary>
    IResponseCookies Cookies { get; }
}
