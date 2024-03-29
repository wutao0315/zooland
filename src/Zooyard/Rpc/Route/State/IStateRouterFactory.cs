﻿namespace Zooyard.Rpc.Route.State;

public interface IStateRouterFactory
{
    string Name { get; }
    /// <summary>
    /// Create state router.
    /// </summary>
    /// <param name="interfaceClass"></param>
    /// <param name="address"></param>
    /// <returns></returns>
    IStateRouter GetRouter(Type interfaceClass, URL address);
    void ClearCache();
}
