using Microsoft.Extensions.Logging;
using Zooyard.WebSocketsImpl.Internal;

namespace Zooyard.WebSocketsImpl.Connections.Internal;

internal class DefaultTransportFactory : ITransportFactory
{
    private readonly HttpClient? _httpClient;
    private readonly WebSocketConnectionOptions _httpConnectionOptions;
    private readonly Func<Task<string?>> _accessTokenProvider;
    private readonly ILoggerFactory _loggerFactory;
    private static volatile bool _websocketsSupported = true;

    public DefaultTransportFactory(ILoggerFactory loggerFactory, HttpClient? httpClient, WebSocketConnectionOptions httpConnectionOptions, Func<Task<string?>> accessTokenProvider)
    {
        if (httpClient == null)
        {
            throw new ArgumentException($"{nameof(httpClient)} cannot be null.", nameof(httpClient));
        }

        _loggerFactory = loggerFactory;
        _httpClient = httpClient;
        _httpConnectionOptions = httpConnectionOptions;
        _accessTokenProvider = accessTokenProvider;
    }

    public ITransport CreateTransport(bool useStatefulReconnect)
    {
        if (_websocketsSupported)
        {
            try
            {
                return new WebSocketsTransport(_httpConnectionOptions, _loggerFactory, _accessTokenProvider, _httpClient, useStatefulReconnect);
            }
            catch (PlatformNotSupportedException)
            {
                _websocketsSupported = false;
            }
        }

        throw new InvalidOperationException("No requested transports available on the server.");
    }
}
