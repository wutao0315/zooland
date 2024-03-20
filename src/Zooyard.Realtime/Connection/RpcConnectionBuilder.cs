using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Net;
using Zooyard.Realtime.TextJson;

namespace Zooyard.Realtime.Connection;

/// <summary>
/// A builder for configuring <see cref="RpcConnection"/> instances.
/// </summary>
public class RpcConnectionBuilder : IRpcConnectionBuilder
{
    private bool _hubConnectionBuilt;

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcConnectionBuilder"/> class.
    /// </summary>
    public RpcConnectionBuilder()
    {
        Services = new ServiceCollection();
        Services.AddSingleton<RpcConnection>();
        Services.AddLogging();
        // todo
        this.AddJsonProtocol();
    }

    /// <inheritdoc />
    public RpcConnection Build()
    {
        // Build can only be used once
        if (_hubConnectionBuilt)
        {
            throw new InvalidOperationException("HubConnectionBuilder allows creation only of a single instance of HubConnection.");
        }

        _hubConnectionBuilt = true;

        // The service provider is disposed by the HubConnection
        var serviceProvider = Services.BuildServiceProvider();

        var connectionFactory = serviceProvider.GetService<IClientConnectionFactory>() ??
            throw new InvalidOperationException($"Cannot create {nameof(RpcConnection)} instance. An {nameof(IClientConnectionFactory)} was not configured.");

        var endPoint = serviceProvider.GetService<EndPoint>() ??
            throw new InvalidOperationException($"Cannot create {nameof(RpcConnection)} instance. An {nameof(EndPoint)} was not configured.");


        return serviceProvider.GetRequiredService<RpcConnection>();
    }

    // Prevents from being displayed in intellisense
    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    // Prevents from being displayed in intellisense
    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    // Prevents from being displayed in intellisense
    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? ToString()
    {
        return base.ToString();
    }

    // Prevents from being displayed in intellisense
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new Type GetType()
    {
        return base.GetType();
    }
}
