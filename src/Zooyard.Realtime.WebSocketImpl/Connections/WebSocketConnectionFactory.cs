using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using Zooyard.Realtime;
using Zooyard.Realtime.Connection;

namespace Zooyard.WebSocketsImpl.Connections;

/// <summary>
/// A factory for creating <see cref="WebSocketConnection"/> instances.
/// </summary>
public class WebSocketConnectionFactory : IClientConnectionFactory
{
    private readonly WebSocketConnectionOptions _httpConnectionOptions;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">The connection options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public WebSocketConnectionFactory(IOptions<WebSocketConnectionOptions> options, ILoggerFactory loggerFactory)
    {
        if (options == null)
        {
            throw new ArgumentNullException($"{nameof(options)} is null");
        }

        _httpConnectionOptions = options.Value;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Creates a new connection to an <see cref="UriEndPoint"/>.
    /// </summary>
    /// <param name="endPoint">The <see cref="UriEndPoint"/> to connect to.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> that represents the asynchronous connect, yielding the <see cref="ConnectionContext" /> for the new connection when completed.
    /// </returns>
    public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
    {
        if (endPoint == null)
        {
            throw new ArgumentNullException($"{nameof(endPoint)} is null");
        }

        if (endPoint is not UriEndPoint uriEndPoint)
        {
            throw new NotSupportedException($"The provided {nameof(EndPoint)} must be of type {nameof(UriEndPoint)}.");
        }

        if (_httpConnectionOptions.Url != null && _httpConnectionOptions.Url != uriEndPoint.Uri)
        {
            throw new InvalidOperationException($"If {nameof(WebSocketConnectionOptions)}.{nameof(WebSocketConnectionOptions.Url)} was set, it must match the {nameof(UriEndPoint)}.{nameof(UriEndPoint.Uri)} passed to {nameof(ConnectAsync)}.");
        }

        // Shallow copy before setting the Url property so we don't mutate the user-defined options object.
        var shallowCopiedOptions = ShallowCopyHttpConnectionOptions(_httpConnectionOptions);
        shallowCopiedOptions.Url = uriEndPoint.Uri;

        var connection = new WebSocketConnection(shallowCopiedOptions, _loggerFactory);

        try
        {
            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            // Make sure the connection is disposed, in case it allocated any resources before failing.
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    // Internal for testing
    internal static WebSocketConnectionOptions ShallowCopyHttpConnectionOptions(WebSocketConnectionOptions options)
    {
        var newOptions = new WebSocketConnectionOptions
        {
            HttpMessageHandlerFactory = options.HttpMessageHandlerFactory,
            Headers = options.Headers,
            Url = options.Url,
            SkipNegotiation = options.SkipNegotiation,
            AccessTokenProvider = options.AccessTokenProvider,
            CloseTimeout = options.CloseTimeout,
            DefaultTransferFormat = options.DefaultTransferFormat,
            ApplicationMaxBufferSize = options.ApplicationMaxBufferSize,
            TransportMaxBufferSize = options.TransportMaxBufferSize,
            UseStatefulReconnect = options.UseStatefulReconnect,
        };

        if (!OperatingSystem.IsBrowser())
        {
            newOptions.Cookies = options.Cookies;
            newOptions.ClientCertificates = options.ClientCertificates;
            newOptions.Credentials = options.Credentials;
            newOptions.Proxy = options.Proxy;
            newOptions.UseDefaultCredentials = options.UseDefaultCredentials;
            newOptions.WebSocketConfiguration = options.WebSocketConfiguration;
            newOptions.WebSocketFactory = options.WebSocketFactory;
        }

        return newOptions;
    }
}
