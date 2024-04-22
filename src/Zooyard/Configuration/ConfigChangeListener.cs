using Microsoft.Extensions.Logging;

namespace Zooyard.Configuration;

public class ConfigChangeListener(ILogger<ConfigChangeListener> logger) : IConfigChangeListener
{
    public void ConfigurationApplied(IReadOnlyList<IRpcConfig> proxyConfigs)
    {
        logger.LogInformation("applied: called");
    }

    /// <summary>
    /// Invoked when an error occurs while loading the configuration.
    /// </summary>
    /// <param name="configProvider">The instance of the configuration provider that failed to provide the configuration.</param>
    /// <param name="exception">The thrown exception.</param>
    public void ConfigurationLoadingFailed(IRpcConfigProvider configProvider, Exception exception)
    {
        logger.LogError(exception, $"loading failed,ex:{exception.Message}");
    }

    /// <summary>
    /// Invoked once the configuration have been successfully loaded.
    /// </summary>
    /// <param name="proxyConfigs">The list of instances that have been loaded.</param>
    public void ConfigurationLoaded(IReadOnlyList<IRpcConfig> proxyConfigs)
    {
        logger.LogInformation("loaded: called");
    }

    /// <summary>
    /// Invoked when an error occurs while applying the configuration.
    /// </summary>
    /// <param name="proxyConfigs">The list of instances that were being processed.</param>
    /// <param name="exception">The thrown exception.</param>
    public void ConfigurationApplyingFailed(IReadOnlyList<IRpcConfig> proxyConfigs, Exception exception)
    {
        logger.LogError(exception, $"applied failed,ex:{exception.Message}");
    }
}
