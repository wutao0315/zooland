namespace Zooyard.Realtime.Server.Internal;

internal sealed class GroupManager<THub> : IGroupManager where THub : Hub
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;

    public GroupManager(HubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager;
    }

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.AddToGroupAsync(connectionId, groupName, cancellationToken);
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
    }
}
