namespace Zooyard.Configuration.ServiceValidators;

internal sealed class InstanceValidator : IServiceValidator
{
    public ValueTask ValidateAsync(ServiceConfig service, IList<Exception> errors)
    {
        if (service.Instances is null)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var (name, instance) in service.Instances)
        {
            if (string.IsNullOrEmpty(instance.Host))
            {
                errors.Add(new ArgumentException($"No host found for instance '{name}' on service '{service.ServiceId}'."));
            }
        }

        return ValueTask.CompletedTask;
    }
}
