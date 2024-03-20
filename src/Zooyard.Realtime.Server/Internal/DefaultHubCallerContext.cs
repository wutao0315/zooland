using System.Security.Claims;

namespace Zooyard.Realtime.Server.Internal;

/// <summary>
/// A context for accessing information about the hub caller from their connection.
/// </summary>
internal sealed class DefaultHubCallerContext : HubCallerContext
{
    private readonly HubConnectionContext _connection;

    public DefaultHubCallerContext(HubConnectionContext connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public override string ConnectionId => _connection.ConnectionId;

    /// <inheritdoc />
    public override string? UserIdentifier => _connection.UserIdentifier;

    /// <inheritdoc />
    public override ClaimsPrincipal? User => _connection.User;

    /// <inheritdoc />
    public override IDictionary<object, object?> Items => _connection.Items;

    /// <inheritdoc />
    public override IFeatureCollection Features => _connection.Features;

    /// <inheritdoc />
    public override CancellationToken ConnectionAborted => _connection.ConnectionAborted;

    /// <inheritdoc />
    public override void Abort() => _connection.Abort();
}
