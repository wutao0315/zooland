using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Reverse Proxy builder interface.
/// </summary>
public interface IReverseProxyBuilder
{
    /// <summary>
    /// Gets the services.
    /// </summary>
    IServiceCollection Services { get; }
}
