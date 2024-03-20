using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Zooyard.Realtime.Connections.Features;


/// <summary>
/// Default implementation for <see cref="IHttpResponseFeature"/>.
/// </summary>
public class HttpResponseFeature : IHttpResponseFeature
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpResponseFeature"/>.
    /// </summary>
    public HttpResponseFeature()
    {
        StatusCode = 200;
        Headers = new HeaderDictionary();
        Body = Stream.Null;
    }

    /// <inheritdoc />
    public int StatusCode { get; set; }

    /// <inheritdoc />
    public string? ReasonPhrase { get; set; }

    /// <inheritdoc />
    public IHeaderDictionary Headers { get; set; }

    /// <inheritdoc />
    public Stream Body { get; set; }

    /// <inheritdoc />
    public virtual bool HasStarted => false;

    /// <inheritdoc />
    public virtual void OnStarting(Func<object, Task> callback, object state)
    {
    }

    /// <inheritdoc />
    public virtual void OnCompleted(Func<object, Task> callback, object state)
    {
    }
}
