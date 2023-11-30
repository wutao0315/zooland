namespace Microsoft.Extensions.DependencyInjection;

public interface IRpcBuilder
{
    /// <summary>
    /// Gets the services.
    /// </summary>
    IServiceCollection Services { get; }
}
