using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Zooyard.Realtime;
using Zooyard.Realtime.Features;
using Zooyard.WebSocketsImpl.Connections;
using Zooyard.WebSocketsImpl.Connections.Internal;

namespace Zooyard.WebSocketsImpl;

/// <summary>
/// Used to make a connection to an ASP.NET Core ConnectionHandler using an HTTP-based transport.
/// </summary>
public partial class WebSocketConnection : ConnectionContext , IConnectionInherentKeepAliveFeature
{
    // Not configurable on purpose, high enough that if we reach here, it's likely
    // a buggy server
    private const int _maxRedirects = 100;
    private const int _protocolVersionNumber = 1;
    private static readonly Task<string?> _noAccessToken = Task.FromResult<string?>(null);

    private static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(120);

    internal readonly ILogger _logger;

    private readonly SemaphoreSlim _connectionLock = new (1, 1);
    private bool _started;
    private bool _disposed;
    private bool _hasInherentKeepAlive;

    private readonly HttpClient? _httpClient;
    private readonly WebSocketConnectionOptions _httpConnectionOptions;
    private ITransport? _transport;
    private readonly ITransportFactory _transportFactory;
    private string? _connectionId;
    private readonly ConnectionLogScope _logScope;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Uri _url;
    private Func<Task<string?>>? _accessTokenProvider;

    /// <inheritdoc />
    public override IDuplexPipe Transport
    {
        get
        {
            CheckDisposed();
            if (_transport == null)
            {
                throw new InvalidOperationException($"Cannot access the {nameof(Transport)} pipe before the connection has started.");
            }
            return _transport;
        }
        set => throw new NotSupportedException("The transport pipe isn't settable.");
    }

    /// <inheritdoc />
    public override IFeatureCollection Features { get; } = new FeatureCollection();

    /// <summary>
    /// Gets or sets the connection ID.
    /// </summary>
    /// <remarks>
    /// The connection ID is set when the <see cref="WebSocketConnection"/> is started and should not be set by user code.
    /// If the connection was created with <see cref="WebSocketConnectionOptions.SkipNegotiation"/> set to <c>true</c>
    /// then the connection ID will be <c>null</c>.
    /// </remarks>
    public override string? ConnectionId
    {
#pragma warning disable CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
        get => _connectionId;
#pragma warning restore CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
        set => throw new InvalidOperationException("The ConnectionId is set internally and should not be set by user code.");
    }

    /// <inheritdoc />
    public override IDictionary<object, object?> Items { get; set; } = new ConnectionItems();

    /// <inheritdoc />
    bool IConnectionInherentKeepAliveFeature.HasInherentKeepAlive => _hasInherentKeepAlive;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnection"/> class.
    /// </summary>
    /// <param name="url">The URL to connect to.</param>
    public WebSocketConnection(Uri url) : this(url, loggerFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnection"/> class.
    /// </summary>
    /// <param name="url">The URL to connect to.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public WebSocketConnection(Uri url, ILoggerFactory? loggerFactory) : this(CreateHttpOptions(url), loggerFactory)
    {
    }

    private static WebSocketConnectionOptions CreateHttpOptions(Uri url)
    {
        if (url == null)
        {
            throw new ArgumentNullException(nameof(url));
        }
        return new WebSocketConnectionOptions { Url = url };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnection"/> class.
    /// </summary>
    /// <param name="httpConnectionOptions">The connection options to use.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public WebSocketConnection(WebSocketConnectionOptions httpConnectionOptions, ILoggerFactory? loggerFactory)
    {
        if (httpConnectionOptions.Url == null)
        {
            throw new ArgumentException("Options does not have a URL specified.", nameof(httpConnectionOptions));
        }

        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        _logger = _loggerFactory.CreateLogger<WebSocketConnection>();
        _httpConnectionOptions = httpConnectionOptions;

        if (!httpConnectionOptions.SkipNegotiation)
        {
            _httpClient = CreateHttpClient();
        }

        _transportFactory = new DefaultTransportFactory(_loggerFactory, _httpClient, httpConnectionOptions, GetAccessTokenAsync);
        _logScope = new ConnectionLogScope();

        Features.Set<IConnectionInherentKeepAliveFeature>(this);
    }

    // Used by unit tests
    internal WebSocketConnection(WebSocketConnectionOptions httpConnectionOptions, ILoggerFactory loggerFactory, ITransportFactory transportFactory)
        : this(httpConnectionOptions, loggerFactory)
    {
        _transportFactory = transportFactory;
    }

    /// <summary>
    /// Starts the connection.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous start.</returns>
    /// <remarks>
    /// A connection cannot be restarted after it has stopped. To restart a connection
    /// a new instance should be created using the same options.
    /// </remarks>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return StartAsync(_httpConnectionOptions.DefaultTransferFormat, cancellationToken);
    }

    /// <summary>
    /// Starts the connection using the specified transfer format.
    /// </summary>
    /// <param name="transferFormat">The transfer format the connection should use.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous start.</returns>
    /// <remarks>
    /// A connection cannot be restarted after it has stopped. To restart a connection
    /// a new instance should be created using the same options.
    /// </remarks>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public async Task StartAsync(TransferFormat transferFormat, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(_logScope))
        {
            await StartAsyncCore(transferFormat, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task StartAsyncCore(TransferFormat transferFormat, CancellationToken cancellationToken)
    {
        CheckDisposed();

        if (_started)
        {
            Log.SkippingStart(_logger);
            return;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CheckDisposed();

            if (_started)
            {
                Log.SkippingStart(_logger);
                return;
            }

            Log.Starting(_logger);

            await SelectAndStartTransport(transferFormat, cancellationToken).ConfigureAwait(false);

            _started = true;
            Log.Started(_logger);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Disposes the connection.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous dispose.</returns>
    /// <remarks>
    /// A connection cannot be restarted after it has stopped. To restart a connection
    /// a new instance should be created using the same options.
    /// </remarks>
    public new async Task DisposeAsync()
    {
        using (_logger.BeginScope(_logScope))
        {
            await DisposeAsyncCore().ForceAsync();
        }
    }

    private async Task DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        await _connectionLock.WaitAsync();
        try
        {
            if (!_disposed && _started)
            {
                Log.DisposingHttpConnection(_logger);

                // Stop the transport, but we don't care if it throws.
                // The transport should also have completed the pipe with this exception.
                try
                {
                    await _transport.StopAsync();
                }
                catch (Exception ex)
                {
                    Log.TransportThrewExceptionOnStop(_logger, ex);
                }

                Log.Disposed(_logger);
            }
            else
            {
                Log.SkippingDispose(_logger);
            }

            _httpClient?.Dispose();
        }
        finally
        {
            // We want to do these things even if the WaitForWriterToComplete/WaitForReaderToComplete fails
            if (!_disposed)
            {
                _disposed = true;
            }

            _connectionLock.Release();
        }
    }

    private async Task SelectAndStartTransport(TransferFormat transferFormat, CancellationToken cancellationToken)
    {
        var uri = _url;
        // Set the initial access token provider back to the original one from options
        _accessTokenProvider = _httpConnectionOptions.AccessTokenProvider;

        var transportExceptions = new List<Exception>();

        await StartTransport(uri, transferFormat, cancellationToken, useStatefulReconnect: false).ConfigureAwait(false);
        Log.StartingTransport(_logger, _transport!.Name, uri);

        if (_transport == null)
        {
            if (transportExceptions.Count > 0)
            {
                throw new AggregateException("Unable to connect to the server with any of the available transports.", transportExceptions);
            }
            else
            {
                throw new Exception("None of the transports supported by the client are supported by the server.");
            }
        }
    }

    private async Task StartTransport(Uri connectUrl, TransferFormat transferFormat,
        CancellationToken cancellationToken, bool useStatefulReconnect)
    {
        // Construct the transport
        var transport = _transportFactory.CreateTransport(useStatefulReconnect);

        // Start the transport, giving it one end of the pipe
        try
        {
            await transport.StartAsync(connectUrl, transferFormat, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.ErrorStartingTransport(_logger, transport.Name, ex);

            _transport = null;
            throw;
        }

        //// Disable keep alives for long polling
        //_hasInherentKeepAlive = transportType == HttpTransportType.LongPolling;

        // We successfully started, set the transport properties (we don't want to set these until the transport is definitely running).
        _transport = transport;

        if (useStatefulReconnect && _transport is IStatefulReconnectFeature reconnectFeature)
        {
#pragma warning disable CA2252 // This API requires opting into preview features
            Features.Set(reconnectFeature);
#pragma warning restore CA2252 // This API requires opting into preview features
        }

        Log.TransportStarted(_logger, transport.Name);
    }

    private HttpClient CreateHttpClient()
    {
        var httpClientHandler = new HttpClientHandler();
        HttpMessageHandler httpMessageHandler = httpClientHandler;

        if (_httpConnectionOptions != null)
        {
            if (_httpConnectionOptions.Proxy != null)
            {
                httpClientHandler.Proxy = _httpConnectionOptions.Proxy;
            }
            if (_httpConnectionOptions.Cookies != null)
            {
                httpClientHandler.CookieContainer = _httpConnectionOptions.Cookies;
            }

            // Only access HttpClientHandler.ClientCertificates if the user has configured client certs
            // Mono does not support client certs and will throw NotImplementedException
            // https://github.com/aspnet/SignalR/issues/2232
            var clientCertificates = _httpConnectionOptions.ClientCertificates;
            if (clientCertificates?.Count > 0)
            {
                httpClientHandler.ClientCertificates.AddRange(clientCertificates);
            }

            if (_httpConnectionOptions.UseDefaultCredentials != null)
            {
                httpClientHandler.UseDefaultCredentials = _httpConnectionOptions.UseDefaultCredentials.Value;
            }
            if (_httpConnectionOptions.Credentials != null)
            {
                httpClientHandler.Credentials = _httpConnectionOptions.Credentials;
            }

            httpMessageHandler = httpClientHandler;
            if (_httpConnectionOptions.HttpMessageHandlerFactory != null)
            {
                httpMessageHandler = _httpConnectionOptions.HttpMessageHandlerFactory(httpClientHandler);
                if (httpMessageHandler == null)
                {
                    throw new InvalidOperationException("Configured HttpMessageHandlerFactory did not return a value.");
                }
            }

            // Apply the authorization header in a handler instead of a default header because it can change with each request
            httpMessageHandler = new AccessTokenHttpMessageHandler(httpMessageHandler, this);
        }

        // Wrap message handler after HttpMessageHandlerFactory to ensure not overridden
        httpMessageHandler = new LoggingHttpMessageHandler(httpMessageHandler, _loggerFactory);

        var httpClient = new HttpClient(httpMessageHandler);
        httpClient.Timeout = HttpClientTimeout;

        // Start with the user agent header
        httpClient.DefaultRequestHeaders.UserAgent.Add(Constants.UserAgentHeader);

        // Apply any headers configured on the HttpConnectionOptions
        if (_httpConnectionOptions?.Headers != null)
        {
            foreach (var header in _httpConnectionOptions.Headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        httpClient.DefaultRequestHeaders.Remove("X-Requested-With");
        // Tell auth middleware to 401 instead of redirecting
        httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        return httpClient;
    }

    internal Task<string?> GetAccessTokenAsync()
    {
        if (_accessTokenProvider == null)
        {
            return _noAccessToken;
        }
        return _accessTokenProvider();
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WebSocketConnection));
        }
    }
}
