using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.State
{
    public interface IStateRouter
    {
        ///// <summary>
        ///// Get the router url.
        ///// </summary>
        //URL Url { get; }
        /// <summary>
        /// Filter invokers with current routing rule and only return the invokers that comply with the rule.
        /// Caching address lists in BitMap mode improves routing performance.
        /// </summary>
        /// <param name="invokers">invoker bit list</param>
        /// <param name="url">refer url</param>
        /// <param name="invocation">invocation</param>
        /// <param name="needToPrintMessage">whether to print router state. Such as `use router branch a`.</param>
        ///// <param name="nodeHolder"></param>
        /// <returns>with route result</returns>
        IList<URL> Route(IList<URL> invokers, URL url, IInvocation invocation, bool needToPrintMessage);//, Holder<RouterSnapshotNode> nodeHolder);

        /// <summary>
        /// To decide whether this router need to execute every time an RPC comes or should only execute when addresses or
        /// rule change.
        /// true if the router need to execute every time.
        /// </summary>
        bool Runtime { get; }

        /// <summary>
        /// To decide whether this router should take effect when none of the invoker can match the router rule, which
        /// means the {@link #route(BitList, URL, Invocation, boolean, Holder)} would be empty. Most of time, most router implementation would
        /// default this value to false.
        /// true to execute if none of invokers matches the current router
        /// </summary>
        bool Force { get; }

        ///// <summary>
        ///// Notify the router the invoker list. Invoker list may change from time to time. This method gives the router a
        ///// chance to prepare before {@link StateRouter#route(BitList, URL, Invocation, boolean, Holder)} gets called.
        ///// No need to notify next node.
        ///// </summary>
        ///// <param name="invokers">invoker list</param>
        //void Notify(BitList<IInvoker> invokers);

        ///// <summary>
        ///// Build Router's Current State Snapshot for QoS
        ///// </summary>
        ///// <returns>Current State</returns>
        //string BuildSnapshot();

        //void Stop()
        //{
        //    //do nothing by default
        //}
        ///// <summary>
        ///// Notify next router node to current router.
        ///// </summary>
        ///// <param name="nextRouter">next router node</param>
        //void SetNextRouter(IStateRouter nextRouter);
    }
}
