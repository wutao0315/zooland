using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Security.Cryptography;

namespace Zooyard.Realtime.Connections.Internal;

internal sealed partial class RpcConnectionManager
{
    // TODO: Consider making this configurable? At least for testing?
    private static readonly TimeSpan _heartbeatTickRate = TimeSpan.FromSeconds(1);

    private readonly ConcurrentDictionary<string, RpcConnectionContext> _connections = new (StringComparer.Ordinal);
    private readonly PeriodicTimer _nextHeartbeat;
    private readonly ILogger<RpcConnectionManager> _logger;
    private readonly ILogger<RpcConnectionContext> _connectionLogger;
    private readonly long _disconnectTimeoutTicks;
    private readonly RpcConnectionsMetrics _metrics;

    public RpcConnectionManager(ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime, IOptions<ConnectionOptions> connectionOptions, RpcConnectionsMetrics metrics)
    {
        _logger = loggerFactory.CreateLogger<RpcConnectionManager>();
        _connectionLogger = loggerFactory.CreateLogger<RpcConnectionContext>();
        _nextHeartbeat = new PeriodicTimer(_heartbeatTickRate);
        _disconnectTimeoutTicks = (long)(connectionOptions.Value.DisconnectTimeout ?? ConnectionOptionsSetup.DefaultDisconectTimeout).TotalMilliseconds;
        _metrics = metrics;

        // Register these last as the callbacks could run immediately
        appLifetime.ApplicationStarted.Register(Start);
        appLifetime.ApplicationStopping.Register(CloseConnections);
    }

    public void Start()
    {
        // Start the timer loop
        _ = ExecuteTimerLoop();
    }

    internal bool TryGetConnection(string id, [NotNullWhen(true)] out RpcConnectionContext? connection)
    {
        return _connections.TryGetValue(id, out connection);
    }

    internal RpcConnectionContext CreateConnection()
    {
        return CreateConnection(new());
    }

    /// <summary>
    /// Creates a connection without Pipes setup to allow saving allocations until Pipes are needed.
    /// </summary>
    /// <returns></returns>
    internal RpcConnectionContext CreateConnection(HttpConnectionDispatcherOptions options, int negotiateVersion = 0, bool useStatefulReconnect = false)
    {
        string connectionToken;
        var id = MakeNewConnectionId();
        if (negotiateVersion > 0)
        {
            connectionToken = MakeNewConnectionId();
        }
        else
        {
            connectionToken = id;
        }

        var metricsContext = _metrics.CreateContext();

        Log.CreatedNewConnection(_logger, id);

        var pair = DuplexPipe.CreateConnectionPair(options.TransportPipeOptions, options.AppPipeOptions);
        var connection = new RpcConnectionContext(id, connectionToken, _connectionLogger, metricsContext, pair.Application, pair.Transport, options, useStatefulReconnect);

        _connections.TryAdd(connectionToken, connection);

        return connection;
    }

    public void RemoveConnection(string id, Microsoft.AspNetCore.Http.Connections.HttpTransportType transportType, RpcConnectionStopStatus status)
    {
        // Remove the connection completely
        if (_connections.TryRemove(id, out var connection))
        {
            // A connection is considered started when the transport is negotiated.
            // You can't stop something that hasn't started so only log connection stop events if there is a transport.
            if (connection.TransportType != Microsoft.AspNetCore.Http.Connections.HttpTransportType.None)
            {
                var currentTimestamp = (connection.StartTimestamp > 0) ? Stopwatch.GetTimestamp() : default;

                RpcConnectionsEventSource.Log.ConnectionStop(id, connection.StartTimestamp, currentTimestamp);
                _metrics.TransportStop(connection.MetricsContext, transportType);
                _metrics.ConnectionStop(connection.MetricsContext, transportType, status, connection.StartTimestamp, currentTimestamp);
            }

            Log.RemovedConnection(_logger, id);
        }
    }

    private static string MakeNewConnectionId()
    {
        // 128 bit buffer / 8 bits per byte = 16 bytes
        Span<byte> buffer = stackalloc byte[16];
        // Generate the id with RNGCrypto because we want a cryptographically random id, which GUID is not
        RandomNumberGenerator.Fill(buffer);
        return Zooyard.Realtime.Internal.WebEncoders.Base64UrlEncode(buffer);
    }

    private async Task ExecuteTimerLoop()
    {
        Log.HeartBeatStarted(_logger);

        // Dispose the timer when all the code consuming callbacks has completed
        using (_nextHeartbeat)
        {
            // The TimerAwaitable will return true until Stop is called
            while (await _nextHeartbeat.WaitForNextTickAsync())
            {
                try
                {
                    Scan();
                }
                catch (Exception ex)
                {
                    Log.ScanningConnectionsFailed(_logger, ex);
                }
            }
        }

        Log.HeartBeatEnded(_logger);
    }

    public void Scan()
    {
        var now = DateTimeOffset.UtcNow;
        var ticks = Environment.TickCount64;

        // Scan the registered connections looking for ones that have timed out
        foreach (var c in _connections)
        {
            var connection = c.Value;
            // Capture the connection state
            var lastSeenTick = connection.LastSeenTicksIfInactive;

            // Once the decision has been made to dispose we don't check the status again
            // But don't clean up connections while the debugger is attached.
            if (!Debugger.IsAttached && lastSeenTick.HasValue && (ticks - lastSeenTick.Value) > _disconnectTimeoutTicks)
            {
                Log.ConnectionTimedOut(_logger, connection.ConnectionId);
                RpcConnectionsEventSource.Log.ConnectionTimedOut(connection.ConnectionId);

                // This is most likely a long polling connection. The transport here ends because
                // a poll completed and has been inactive for > 5 seconds so we wait for the
                // application to finish gracefully
                _ = DisposeAndRemoveAsync(connection, closeGracefully: true, RpcConnectionStopStatus.Timeout);
            }
            else
            {
                if (!Debugger.IsAttached)
                {
                    connection.TryCancelSend(ticks);
                }

                // Tick the heartbeat, if the connection is still active
                connection.TickHeartbeat();

                if (connection.IsAuthenticationExpirationEnabled && connection.AuthenticationExpiration < now &&
                    !connection.ConnectionClosedRequested.IsCancellationRequested)
                {
                    Log.AuthenticationExpired(_logger, connection.ConnectionId);
                    connection.RequestClose();
                }
            }
        }
    }

    public void CloseConnections()
    {
        // Stop firing the timer
        _nextHeartbeat.Dispose();

        var tasks = new List<Task>(_connections.Count);

        // REVIEW: In the future we can consider a hybrid where we first try to wait for shutdown
        // for a certain time frame then after some grace period we shutdown more aggressively
        foreach (var c in _connections)
        {
            // We're shutting down so don't wait for closing the application
            tasks.Add(DisposeAndRemoveAsync(c.Value, closeGracefully: false, RpcConnectionStopStatus.AppShutdown));
        }

        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
    }

    internal async Task DisposeAndRemoveAsync(RpcConnectionContext connection, bool closeGracefully, RpcConnectionStopStatus status)
    {
        try
        {
            await connection.DisposeAsync(closeGracefully);
        }
        catch (IOException ex)
        {
            Log.ConnectionReset(_logger, connection.ConnectionId, ex);
        }
        catch (WebSocketException ex) when (ex.InnerException is IOException)
        {
            Log.ConnectionReset(_logger, connection.ConnectionId, ex);
        }
        catch (Exception ex)
        {
            Log.FailedDispose(_logger, connection.ConnectionId, ex);
        }
        finally
        {
            // Remove it from the list after disposal so that's it's easy to see
            // connections that might be in a hung state via the connections list
            RemoveConnection(connection.ConnectionToken, connection.TransportType, status);
        }
    }
}
