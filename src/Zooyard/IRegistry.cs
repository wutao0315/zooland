namespace Zooyard;

public interface IRegistryService
{
    /// <summary>
    /// registry
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    Task RegisterService(URL url);
    /// <summary>
    /// unregistry
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    Task UnregisterService(URL url);
    Task Subscribe(URL url, INotifyListener listener);
    Task Unsubscribe(URL url, INotifyListener listener);
    Task<List<URL>> Lookup(URL url);
}

public interface INotifyListener
{
    /// <summary>
    /// Triggered when a service change notification is received.
    /// </summary>
    /// <remarks>
    /// Notify needs to support the contract: <br>
    /// 1. Always notifications on the service interface and the dimension of the data type.that is, won't notify part of the same type data belonging to one service. Users do not need to compare the results of the previous notification.<br>
    /// 2. The first notification at a subscription must be a full notification of all types of data of a service.<br>
    /// 3. At the time of change, different types of data are allowed to be notified separately, e.g.: providers, consumers, routers, overrides.It allows only one of these types to be notified, but the data of this type must be full, not incremental.<br>
    /// 4. If a data type is empty, need to notify a empty protocol with category parameter identification of url data.<br>
    /// 5. The order of notifications to be guaranteed by the notifications(That is, the implementation of the registry). Such as: single thread push, queue serialization, and version comparison.<br>
    /// </remarks>
    /// <param name="urls">The list of registered information , is always not empty. The meaning is the same as the return value of {@link IRegistry#Lookup(URL)}.</param>
    void Notify(List<URL> urls);
}
