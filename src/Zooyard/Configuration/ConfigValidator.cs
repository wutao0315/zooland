using Zooyard.Configuration.RouteValidators;
using Zooyard.Configuration.ServiceValidators;

namespace Zooyard.Configuration;

internal sealed class ConfigValidator : IConfigValidator
{

    private readonly IRouteValidator[] _routeValidators;
    private readonly IServiceValidator[] _serviceValidators;

    public ConfigValidator(
        IEnumerable<IRouteValidator> routeValidators,
        IEnumerable<IServiceValidator> serviceValidators)
    {
        _routeValidators = routeValidators?.ToArray() ?? throw new ArgumentNullException(nameof(routeValidators));
        _serviceValidators = serviceValidators?.ToArray() ?? throw new ArgumentNullException(nameof(serviceValidators));
    }


    // Note this performs all validation steps without short circuiting in order to report all possible errors.
    public async ValueTask<IList<Exception>> ValidateRouteAsync(RouteConfig pub)
    {
        _ = pub ?? throw new ArgumentNullException(nameof(pub));
        var errors = new List<Exception>();

        if (string.IsNullOrEmpty(pub.RouteId))
        {
            errors.Add(new ArgumentException("Missing Route Id."));
        }

        foreach (var routeValidator in _routeValidators)
        {
            await routeValidator.ValidateAsync(pub, errors);
        }

        return errors;
    }


    // Note this performs all validation steps without short circuiting in order to report all possible errors.
    public async ValueTask<IList<Exception>> ValidateServiceAsync(ServiceConfig service)
    {
        _ = service ?? throw new ArgumentNullException(nameof(service));
        var errors = new List<Exception>();

        if (string.IsNullOrEmpty(service.ServiceId))
        {
            errors.Add(new ArgumentException("Missing Service Id."));
        }

        foreach (var serviceValidator in _serviceValidators)
        {
            await serviceValidator.ValidateAsync(service, errors);
        }

        return errors;
    }
}
