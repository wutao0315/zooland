using Zooyard.Rpc.Route;

namespace Zooyard.Rpc;

public interface IRoute : IComparable<IRoute>
{
    //int DEFAULT_PRIORITY { get; } = int.MaxValue;
    ///// <summary>
    ///// Get the router url.
    ///// </summary>
    ///// <returns></returns>
    //URL Url { get; }
    /// <summary>
    /// This method can return the state of whether routerChain needed to continue route. **
    /// Filter invokers with current routing rule and only return the invokers that comply with the rule.
    /// </summary>
    /// <param name="invokers"></param>
    /// <param name="address"></param>
    /// <param name="invocation"></param>
    /// <param name="needToPrintMessage"></param>
    /// <returns></returns>
    RouterResult<URL> Route(List<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage) 
    {
        return new RouterResult<URL>(null);
    }

    ///// <summary>
    ///// Notify the router the invoker list. Invoker list may change from time to time. This method gives the router a
    ///// chance to prepare before {@link Router#route(List, URL, Invocation)} gets called.
    ///// </summary>
    ///// <param name="invokers">invoker list</param>
    //void Notify(List<IInvoker> invokers)
    //{

    //}
    /// <summary>
    /// To decide whether this router need to execute every time an RPC comes or should only execute when addresses or
    /// rule change.
    /// </summary>
    /// <returns>true if the router need to execute every time.</returns>
    bool Runtime { get; }

    /// <summary>
    /// To decide whether this router should take effect when none of the invoker can match the router rule, which
    /// means the {@link #route(List, URL, Invocation)} would be empty. Most of time, most router implementation would
    /// default this value to false.
    /// </summary>
    /// <returns>true to execute if none of invokers matches the current router</returns>
    bool Force { get; }

    /// <summary>
    /// Router's priority, used to sort routers.
    /// </summary>
    /// <returns></returns>
    int Priority { get; }
    void Stop()
    {
        //do nothing by default
    }

    
}
