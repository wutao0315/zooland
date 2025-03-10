namespace Zooyard.Configuration;

/// <summary>
/// Allows subscribing to events notifying you when the configuration is loaded and applied, or when those actions fail.
/// </summary>
public interface IConfigChangeListener
{
    /// <summary>
    /// Invoked when an error occurs while loading the configuration.
    /// </summary>
    /// <param name="configProvider">The instance of the configuration provider that failed to provide the configuration.</param>
    /// <param name="exception">The thrown exception.</param>
    public void ConfigurationLoadingFailed(IRpcConfigProvider configProvider, Exception exception) 
    {
    }

    /// <summary>
    /// Invoked once the configuration have been successfully loaded.
    /// </summary>
    /// <param name="proxyConfigs">The list of instances that have been loaded.</param>
    public void ConfigurationLoaded(IReadOnlyList<IRpcConfig> proxyConfigs) 
    {
    }

    /// <summary>
    /// Invoked when an error occurs while applying the configuration.
    /// </summary>
    /// <param name="proxyConfigs">The list of instances that were being processed.</param>
    /// <param name="exception">The thrown exception.</param>
    void ConfigurationApplyingFailed(IReadOnlyList<IRpcConfig> proxyConfigs, Exception exception) 
    {
    }

    /// <summary>
    /// Invoked once the configuration has been successfully applied.
    /// </summary>
    /// <param name="proxyConfigs">The list of instances that have been applied.</param>
    void ConfigurationApplied(IReadOnlyList<IRpcConfig> proxyConfigs) 
    {
    }
}
