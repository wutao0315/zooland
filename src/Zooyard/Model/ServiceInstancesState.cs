namespace Zooyard.Model;

public sealed class ServiceInstancesState(
        IReadOnlyList<InstanceState> allInstances,
        IReadOnlyList<InstanceState> availableInstances)
{
    public IReadOnlyList<InstanceState> AllInstances { get; } = allInstances ?? throw new ArgumentNullException(nameof(allInstances));
    public IReadOnlyList<InstanceState> AvailableInstances { get; } = availableInstances ?? throw new ArgumentNullException(nameof(availableInstances));
}
