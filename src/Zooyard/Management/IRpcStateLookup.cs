using Zooyard.Model;
using System.Diagnostics.CodeAnalysis;

namespace Zooyard.Management;

/// <summary>
/// Allows access to the proxy's current set of routes and clusters.
/// </summary>
public interface IRpcStateLookup
{

    /// <summary>
    /// Retrieves a specific public by id, if present.
    /// </summary>
    bool TryGetRoute(string id, [NotNullWhen(true)] out RouteModel? route);

    /// <summary>
    /// Enumerates all current routes. This is thread safe but the collection may change mid enumeration if the configuration is reloaded.
    /// </summary>
    IEnumerable<RouteModel> GetRoutes();

    /// <summary>
    /// Retrieves a specific service by id, if present.
    /// </summary>
    bool TryGetService(string id, [NotNullWhen(true)] out ServiceState? cluster);

    /// <summary>
    /// Enumerates all current services. This is thread safe but the collection may change mid enumeration if the configuration is reloaded.
    /// </summary>
    IEnumerable<ServiceState> GetServices();

    /// <summary>
    /// 监听配置是否发生改变
    /// </summary>
    /// <param name="listener"></param>
    /// <returns></returns>
    IDisposable OnChange(Action<IRpcStateLookup> listener);
}


public static class RpcStateLookupExtend 
{
    public static T GetGlobalMataValue<T>(this IRpcStateLookup lookup,  string key, T defaultVal = default!) 
        where T : IConvertible
    {
        if (!lookup.TryGetRoute("global", out var model)) 
        {
            return defaultVal;
        }

        return model.Config.Metadata.GetValue<T>(key, defaultVal);
    }
}