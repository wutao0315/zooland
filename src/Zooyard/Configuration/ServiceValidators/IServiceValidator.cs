namespace Zooyard.Configuration.ServiceValidators;

/// <summary>
/// Provides method to validate service configuration.
/// </summary>
public interface IServiceValidator
{
    /// <summary>
    /// Perform validation on a service configuration by adding exceptions to the provided collection.
    /// </summary>
    /// <param name="service">Service configuration to validate</param>
    /// <param name="errors">Collection of all validation exceptions</param>
    /// <returns>A ValueTask representing the asynchronous validation operation.</returns>
    public ValueTask ValidateAsync(ServiceConfig service, IList<Exception> errors);
}
