using Microsoft.Extensions.DependencyInjection;

namespace Zooyard.Management;

internal sealed class RpcBuilder(IServiceCollection services) : IRpcBuilder
{
    /// <summary>
    /// Gets the services collection.
    /// </summary>
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
}
