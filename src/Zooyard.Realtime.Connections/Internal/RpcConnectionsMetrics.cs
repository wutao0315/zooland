using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Zooyard.Realtime.Connections.Internal;

internal readonly struct MetricsContext
{
    public MetricsContext(bool connectionDurationEnabled, bool currentConnectionsCounterEnabled)
    {
        ConnectionDurationEnabled = connectionDurationEnabled;
        CurrentConnectionsCounterEnabled = currentConnectionsCounterEnabled;
    }

    public bool ConnectionDurationEnabled { get; }
    public bool CurrentConnectionsCounterEnabled { get; }
}

internal sealed class RpcConnectionsMetrics : IDisposable
{
    public const string MeterName = "Zooyard.Realtime.Connections";

    private readonly Meter _meter;
    private readonly Histogram<double> _connectionDuration;
    private readonly UpDownCounter<long> _currentConnectionsCounter;

    public RpcConnectionsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _connectionDuration = _meter.CreateHistogram<double>(
            "signalr.server.connection.duration",
            unit: "s",
            description: "The duration of connections on the server.");

        _currentConnectionsCounter = _meter.CreateUpDownCounter<long>(
            "signalr.server.active_connections",
            unit: "{connection}",
            description: "Number of connections that are currently active on the server.");
    }

    public void ConnectionStop(in MetricsContext metricsContext, Microsoft.AspNetCore.Http.Connections.HttpTransportType transportType, RpcConnectionStopStatus status, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.ConnectionDurationEnabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _connectionDuration.Record(duration.TotalSeconds,
                new KeyValuePair<string, object?>("signalr.connection.status", ResolveStopStatus(status)),
                new KeyValuePair<string, object?>("signalr.transport", ResolveTransportType(transportType)));
        }
    }

    public void ConnectionTransportStart(in MetricsContext metricsContext, Microsoft.AspNetCore.Http.Connections.HttpTransportType transportType)
    {
        Debug.Assert(transportType != Microsoft.AspNetCore.Http.Connections.HttpTransportType.None);

        // Tags must match transport end.
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            _currentConnectionsCounter.Add(1, new KeyValuePair<string, object?>("signalr.transport", ResolveTransportType(transportType)));
        }
    }

    public void TransportStop(in MetricsContext metricsContext, Microsoft.AspNetCore.Http.Connections.HttpTransportType transportType)
    {
        if (metricsContext.CurrentConnectionsCounterEnabled)
        {
            // Tags must match transport start.
            // If the transport type is none then the transport was never started for this connection.
            if (transportType != Microsoft.AspNetCore.Http.Connections.HttpTransportType.None)
            {
                _currentConnectionsCounter.Add(-1, new KeyValuePair<string, object?>("signalr.transport", ResolveTransportType(transportType)));
            }
        }
    }

    private static string ResolveTransportType(Microsoft.AspNetCore.Http.Connections.HttpTransportType transportType)
    {
        return transportType switch
        {
            Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents => "server_sent_events",
            Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling => "long_polling",
            Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets => "web_sockets",
            _ => throw new InvalidOperationException("Unexpected value: " + transportType)
        };
    }

    private static string ResolveStopStatus(RpcConnectionStopStatus connectionStopStatus)
    {
        return connectionStopStatus switch
        {
            RpcConnectionStopStatus.NormalClosure => "normal_closure",
            RpcConnectionStopStatus.Timeout => "timeout",
            RpcConnectionStopStatus.AppShutdown => "app_shutdown",
            _ => throw new InvalidOperationException("Unexpected value: " + connectionStopStatus)
        };
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public MetricsContext CreateContext()
    {
        return new MetricsContext(_connectionDuration.Enabled, _currentConnectionsCounter.Enabled);
    }
}
