using Zooyard.Utils;

namespace Zooyard.Rpc.Route.State;

public abstract class AbstractStateRouter: IStateRouter
{
    private volatile bool force = false;
    private volatile IStateRouter? nextRouter = null;

    //Should continue route if current router's result is empty
    private readonly bool _shouldFailFast;

    public AbstractStateRouter(URL address)
    {
        Address = address;
        _shouldFailFast = address.GetParameter(Constants.SHOULD_FAIL_FAST_KEY, true);
    }
    public virtual URL Address { get; set; }
    public virtual bool Runtime => true;
    public virtual bool Force { get => force; set => force = value; }
    public IStateRouter? NextRouter { get=> nextRouter; set=> nextRouter = value; }

    public IList<URL> Route(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode>? nodeHolder) 
    {
        if (needToPrintMessage && (nodeHolder == null || nodeHolder.Value == null))
        {
            needToPrintMessage = false;
        }

        RouterSnapshotNode? currentNode = null;
        RouterSnapshotNode? parentNode = null;
        Holder<string>? messageHolder = null;

        // pre-build current node
        if (needToPrintMessage)
        {
            parentNode = nodeHolder!.Value;
            currentNode = new RouterSnapshotNode(this.GetType().Name, invokers.ToList());
            parentNode!.AppendNode(currentNode);

            // set parent node's output size in the first child invoke
            // initial node output size is zero, first child will override it
            if (parentNode.NodeOutputInvokers.Count < invokers.Count)
            {
                parentNode.NodeOutputInvokers = invokers.ToList();
            }

            messageHolder = new Holder<string>();
            nodeHolder.Value = currentNode;
        }

        IList<URL> routeResult = new List<URL>();

        // check if router support call continue route by itself
        if (!SupportContinueRoute())
        {
            routeResult = DoRoute(invokers, address, invocation, needToPrintMessage, nodeHolder!, messageHolder!);
            var shouldFailFast = address.GetParameter(Constants.SHOULD_FAIL_FAST_KEY, true);
            // use current node's result as next node's parameter
            if (!shouldFailFast || routeResult.Count>0)
            {
                routeResult = ContinueRoute(routeResult, address, invocation, needToPrintMessage, nodeHolder!);
            }
        }

        // post-build current node
        if (needToPrintMessage)
        {
            currentNode!.RouterMessage = messageHolder!.Value!;
            if (currentNode.NodeOutputInvokers.Count == 0)
            {
                // no child call
                currentNode.NodeOutputInvokers = routeResult.ToList();
            }
            currentNode.ChainOutputInvokers = routeResult.ToList();
            //set
            nodeHolder!.Value = parentNode;
        }
        return routeResult;
    }

    /// <summary>
    /// Filter invokers with current routing rule and only return the invokers that comply with the rule.
    /// </summary>
    /// <param name="invokers">all invokers to be routed</param>
    /// <param name="address">consumerUrl</param>
    /// <param name="invocation">invocation</param>
    /// <param name="needToPrintMessage">should current router print message</param>
    /// <param name="nodeHolder">node holder</param>
    /// <param name="messageHolder">message holder</param>
    /// <returns></returns>
    protected abstract IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder,Holder<string> messageHolder);


    /// <summary>
    /// Call next router to get result
    /// </summary>
    /// <param name="invokers">current router filtered invokers</param>
    /// <param name="address"></param>
    /// <param name="invocation"></param>
    /// <param name="needToPrintMessage"></param>
    /// <param name="nodeHolder"></param>
    /// <returns></returns>
    protected IList<URL> ContinueRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder)
    {
        if (nextRouter != null)
        {
            return nextRouter.Route(invokers, address, invocation, needToPrintMessage, nodeHolder);
        }
        else
        {
            return invokers;
        }
    }
    /// <summary>
    /// Whether current router's implementation support call
    /// by router itself.
    /// </summary>
    /// <returns></returns>
    protected virtual bool SupportContinueRoute() => false;
    /// <summary>
    /// Next Router node state is maintained by AbstractStateRouter and this method is not allow to override.
    /// If a specified router wants to control the behaviour of continue route or not,
    /// please override {@link AbstractStateRouter#SupportContinueRoute()}
    /// </summary>
    /// <param name="nextRouter"></param>
    public void SetNextRouter(IStateRouter nextRouter)
    {
        this.nextRouter = nextRouter;
    }

    public virtual void Dispose()
    {
        
    }

    //public string BuildSnapshot()
    //{
    //    return DoBuildSnapshot() +
    //        "            ↓ \n" +
    //        nextRouter.BuildSnapshot();
    //}

    //protected string DoBuildSnapshot()
    //{
    //    return this.GetType().Name+ " not support\n";
    //}
}
