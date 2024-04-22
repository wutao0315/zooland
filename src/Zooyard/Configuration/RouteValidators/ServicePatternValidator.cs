namespace Zooyard.Configuration.RouteValidators;

internal sealed class ServicePatternValidator : IRouteValidator
{
    public ValueTask ValidateAsync(RouteConfig publicConfig, IList<Exception> errors)
    {
        if (string.IsNullOrEmpty(publicConfig.ServicePattern))
        {
            // ServicePattern is optional
            return ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }
}
